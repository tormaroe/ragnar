import os
import sys
import json
import subprocess
from google import genai
from google.genai import types

original_files = {} # path -> original content
created_files = []

def harvest_codebase():
    files_context = []
    # Read GEMINI.md for context if it exists
    if os.path.exists("GEMINI.md"):
        try:
            with open("GEMINI.md", "r", encoding="utf-8") as f:
                files_context.append(f"=== FILE: GEMINI.md ===\n{f.read()}\n=== END FILE ===")
        except Exception as e:
            print(f"Warning: Could not read GEMINI.md: {e}")
            
    # Traverse src and test folders
    for folder in ["src", "test"]:
        if not os.path.exists(folder):
            continue
        for root, dirs, files in os.walk(folder):
            # Skip build/cache/git directories
            if "bin" in dirs:
                dirs.remove("bin")
            if "obj" in dirs:
                dirs.remove("obj")
            for file in files:
                # Only harvest relevant source/config files
                if file.endswith((".cs", ".csproj", ".json", ".yml", ".txt")):
                    path = os.path.join(root, file)
                    rel_path = os.path.relpath(path).replace("\\", "/")
                    try:
                        with open(path, "r", encoding="utf-8") as f:
                            content = f.read()
                        files_context.append(f"=== FILE: {rel_path} ===\n{content}\n=== END FILE ===")
                    except Exception as e:
                        print(f"Warning: Could not read {rel_path}: {e}")
                        
    return "\n\n".join(files_context)

def backup_and_apply_changes(changes):
    # First, revert any previous attempt's changes
    restore_backup()
    
    applied = []
    try:
        for change in changes:
            path = change.get("file_path")
            action = change.get("action")
            
            if not path:
                continue
                
            # Normalize path
            path = path.replace("\\", "/")
            if os.path.isabs(path) or ".." in path:
                raise ValueError(f"Invalid or unsafe file path: {path}")
                
            if action == "create":
                if os.path.exists(path):
                    if path not in original_files:
                        with open(path, "r", encoding="utf-8") as f:
                            original_files[path] = f.read()
                else:
                    if path not in created_files:
                        created_files.append(path)
                        
                content = change.get("content", "")
                os.makedirs(os.path.dirname(path), exist_ok=True)
                with open(path, "w", encoding="utf-8") as f:
                    f.write(content)
                applied.append(f"Created file: {path}")
                
            elif action == "modify":
                if not os.path.exists(path):
                    raise FileNotFoundError(f"File to modify does not exist: {path}")
                    
                if path not in original_files:
                    with open(path, "r", encoding="utf-8") as f:
                        original_files[path] = f.read()
                        
                content = original_files[path]
                edits = change.get("edits", [])
                
                for edit in edits:
                    find_str = edit.get("find")
                    replace_str = edit.get("replace")
                    
                    if find_str not in content:
                        raise ValueError(
                            f"Exact match not found in '{path}' for block:\n"
                            f"--- FIND ---\n{find_str}\n------------\n"
                            f"Make sure you match the exact whitespace and characters."
                        )
                    # Replace only the first occurrence to be safe
                    content = content.replace(find_str, replace_str, 1)
                    
                with open(path, "w", encoding="utf-8") as f:
                    f.write(content)
                applied.append(f"Modified file: {path}")
                
            elif action == "delete":
                if os.path.exists(path):
                    if path not in original_files:
                        with open(path, "r", encoding="utf-8") as f:
                            original_files[path] = f.read()
                    os.remove(path)
                    applied.append(f"Deleted file: {path}")
        return applied
    except Exception as e:
        restore_backup()
        raise e

def restore_backup():
    for path, content in original_files.items():
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
    for path in created_files:
        if os.path.exists(path):
            os.remove(path)
    original_files.clear()
    created_files.clear()

def run_tests():
    print("Running dotnet test...")
    result = subprocess.run(["dotnet", "test"], capture_output=True, text=True)
    output = result.stdout + "\n" + result.stderr
    # Limit length of test output to avoid token bloat
    if len(output) > 8000:
        output = "[... truncated ...]\n" + output[-8000:]
    return result.returncode == 0, output

def main():
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        print("Error: GEMINI_API_KEY environment variable is not set.")
        sys.exit(1)
        
    issue_title = ""
    issue_body = ""
    
    # Read issue payload in GitHub Actions
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    if event_path and os.path.exists(event_path):
        try:
            with open(event_path, "r", encoding="utf-8") as f:
                event_data = json.load(f)
            issue = event_data.get("issue", {})
            issue_title = issue.get("title", "")
            issue_body = issue.get("body", "")
        except Exception as e:
            print(f"Warning: Failed to read event JSON: {e}")
            
    # Fallback to env vars (useful for manual/local testing)
    if not issue_title:
        issue_title = os.environ.get("ISSUE_TITLE", "")
    if not issue_body:
        issue_body = os.environ.get("ISSUE_BODY", "")
        
    if not issue_title:
        print("Error: No issue title provided (via GITHUB_EVENT_PATH or ISSUE_TITLE).")
        sys.exit(1)
        
    print(f"Processing Issue: {issue_title}")
    
    print("Gathering codebase context...")
    codebase = harvest_codebase()
    
    client = genai.Client(api_key=api_key)
    
    system_instruction = (
        "You are an autonomous AI software engineer for the Ragnar programming language project.\n"
        "Ragnar is a programming language hosted in .NET/C#, with syntax and semantics based on the Rebol language.\n"
        "You are given the codebase context, an issue description, and your goal is to solve the issue.\n\n"
        "You must only return a valid JSON object matching this schema:\n"
        "{\n"
        '  "explanation": "A brief explanation of the changes made and the design decisions.",\n'
        '  "changes": [\n'
        "    {\n"
        '      "file_path": "relative/path/to/file",\n'
        '      "action": "create" | "modify" | "delete",\n'
        '      "content": "Full content of the new file (only required for \'create\')",\n'
        '      "edits": [\n'
        "        {\n"
        '          "find": "Exact block of code to find (including correct leading whitespace and indentation). Make sure this block is unique in the file.",\n'
        '          "replace": "The replacement code block (with correct indentation)."\n'
        "        }\n"
        "      ] (only required for 'modify')\n"
        "    }\n"
        "  ]\n"
        "}\n\n"
        "Strict Rules:\n"
        "1. Do NOT include markdown code fences (like ```json or ```) in your response. Output raw JSON.\n"
        "2. For 'modify', the 'find' block MUST match the file contents EXACTLY, character-for-character, including all whitespace, tabs, and newlines. If it doesn't match exactly, the script will fail.\n"
        "3. Keep 'find' blocks large enough to be unique, but small enough to represent a single logical change. Do not replace the entire file if you only need to change a few lines.\n"
        "4. When writing code, follow C# / .NET conventions and existing patterns in the codebase.\n"
        "5. If the tests or build fail, you will be given the compiler/test error output. You must output a new set of JSON changes to fix the error.\n"
    )
    
    user_prompt = (
        f"Here is the issue description:\n"
        f"Title: {issue_title}\n"
        f"Description: {issue_body}\n\n"
        f"Here is the codebase context:\n"
        f"{codebase}\n\n"
        f"Please analyze the codebase and generate the changes required to solve the issue."
    )
    
    chat = client.chats.create(
        model="gemini-2.5-flash",
        config=types.GenerateContentConfig(
            system_instruction=system_instruction,
            response_mime_type="application/json",
            temperature=0.2,
        )
    )
    
    current_prompt = user_prompt
    max_attempts = 3
    
    for attempt in range(1, max_attempts + 1):
        print(f"\n--- Attempt {attempt} of {max_attempts} ---")
        try:
            print("Sending request to Gemini...")
            response = chat.send_message(current_prompt)
            response_text = response.text.strip()
            
            # Extract JSON block if output includes fences despite system prompt instructions
            clean_json = response_text
            if clean_json.startswith("```"):
                lines = clean_json.splitlines()
                if lines[0].startswith("```json") or lines[0].startswith("```"):
                    clean_json = "\n".join(lines[1:-1])
            
            try:
                data = json.loads(clean_json)
                explanation = data.get("explanation", "No explanation provided.")
                print(f"Gemini Explanation: {explanation}")
                changes = data.get("changes", [])
            except Exception as e:
                print(f"Failed to parse JSON response: {e}")
                print(f"Raw response:\n{response_text}")
                current_prompt = (
                    f"Your response was not valid JSON or did not match the required schema. Error: {e}.\n"
                    f"Please respond with raw JSON only, matching the exact format specified."
                )
                continue
                
            print(f"Applying {len(changes)} changes...")
            try:
                applied = backup_and_apply_changes(changes)
                for detail in applied:
                    print(f"  {detail}")
            except Exception as e:
                print(f"Failed to apply changes: {e}")
                current_prompt = (
                    f"Failed to apply the changes to the codebase. Error: {e}.\n"
                    f"This usually means a 'find' block did not match the file contents exactly.\n"
                    f"Please review the original file contents and make sure your 'find' blocks are exact character-for-character matches."
                )
                continue
                
            success, test_output = run_tests()
            if success:
                print("Success! Tests passed.")
                # Exit with success, leaving changes applied
                sys.exit(0)
            else:
                print("Tests failed!")
                print(test_output[:1000] + "\n...")
                current_prompt = (
                    f"The changes were applied successfully, but running the tests failed with the following error:\n"
                    f"{test_output}\n\n"
                    f"Please review the errors and output a new complete set of changes to solve the issue and fix the test failures."
                )
                
        except Exception as e:
            print(f"An unexpected error occurred during execution: {e}")
            sys.exit(1)
            
    print(f"\nFailed to resolve the issue after {max_attempts} attempts.")
    restore_backup()
    sys.exit(1)

if __name__ == "__main__":
    main()
