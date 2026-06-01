import os
import sys
import shutil
import subprocess
import xml.etree.ElementTree as ET

def get_csproj_version(csproj_path):
    try:
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        for prop_group in root.findall('.//PropertyGroup'):
            version_elem = prop_group.find('Version')
            if version_elem is not None and version_elem.text:
                return version_elem.text.strip()
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}")
    return None

def clean_directory(dir_path):
    if os.path.exists(dir_path):
        print(f"Cleaning directory: {dir_path}...")
        for item in os.listdir(dir_path):
            item_path = os.path.join(dir_path, item)
            try:
                if os.path.isdir(item_path):
                    shutil.rmtree(item_path)
                else:
                    os.unlink(item_path)
            except Exception as e:
                print(f"Error deleting {item_path}: {e}")
    else:
        os.makedirs(dir_path, exist_ok=True)

def check_command(cmd):
    return shutil.which(cmd) is not None

def run_command(cmd):
    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.returncode, result.stdout, result.stderr

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root_dir = os.path.dirname(script_dir)
    csproj_path = os.path.join(root_dir, 'src', 'Ragnar.csproj')

    if not os.path.exists(csproj_path):
        print(f"Could not find src/Ragnar.csproj at path: {csproj_path}")
        sys.exit(1)

    version = get_csproj_version(csproj_path)
    if not version:
        print(f"Could not find <Version> element in {csproj_path}")
        sys.exit(1)

    print(f"Detected version: {version}")

    rids = ["win-x64", "linux-x64", "osx-x64", "osx-arm64"]
    dist_dir = os.path.join(root_dir, "dist")
    clean_directory(dist_dir)

    # Clean existing release ZIPs for this version in root directory
    for item in os.listdir(root_dir):
        if item.startswith(f"Ragnar_{version}_") and item.endswith(".zip"):
            try:
                os.unlink(os.path.join(root_dir, item))
                print(f"Cleaned existing release ZIP: {item}")
            except Exception as e:
                print(f"Error deleting zip {item}: {e}")

    created_zips = []
    try:
        # Build and package each platform
        for rid in rids:
            print(f"\n=== Building and Packaging for {rid} ===")
            out_dir = os.path.join(dist_dir, rid)

            publish_cmd = [
                "dotnet", "publish", csproj_path,
                "-c", "Release",
                "-r", rid,
                "-o", out_dir,
                "-p:PublishSingleFile=true",
                "-p:PublishReadyToRun=true",
                "-p:SelfContained=false",
                "--no-self-contained"
            ]

            print(f"Running dotnet publish for {rid}...")
            ret, stdout, stderr = run_command(publish_cmd)
            if ret != 0:
                print(f"Failed to publish for {rid}:\n{stderr}")
                sys.exit(1)

            zip_name = f"Ragnar_{version}_{rid}"
            zip_path = os.path.join(root_dir, zip_name)
            zip_file = f"{zip_path}.zip"

            print(f"Creating zip package: {zip_name}.zip...")
            try:
                shutil.make_archive(zip_path, 'zip', out_dir)
                created_zips.append(zip_file)
                print(f"Zip package for {rid} created successfully.")
            except Exception as e:
                print(f"Failed to create zip package: {e}")
                sys.exit(1)

        # Check gh CLI status
        if check_command("gh"):
            print("\nGitHub CLI (gh) detected. Checking auth status...")
            ret, stdout, stderr = run_command(["gh", "auth", "status"])
            if ret == 0:
                print(f"Creating GitHub Release v{version} and uploading zip packages...")
                
                zip_files = []
                for item in os.listdir(root_dir):
                    if item.startswith(f"Ragnar_{version}_") and item.endswith(".zip"):
                        zip_files.append(os.path.join(root_dir, item))

                release_cmd = ["gh", "release", "create", f"v{version}"] + zip_files + [
                    "--title", f"v{version}",
                    "--generate-notes"
                ]

                ret, stdout, stderr = run_command(release_cmd)
                if ret == 0:
                    print(f"GitHub Release v{version} successfully created and assets uploaded.")
                else:
                    print(f"Failed to create GitHub Release:\n{stderr}")
                    sys.exit(1)
            else:
                print("Warning: GitHub CLI is not authenticated. Skipping release creation on GitHub.")
                print("Run 'gh auth login' to authenticate if you want to publish releases automatically.")
        else:
            print("Warning: GitHub CLI (gh) was not found in PATH. Skipping release creation on GitHub.")
    finally:
        if created_zips:
            print("\n=== Cleaning up local release ZIP files ===")
            for zip_file in created_zips:
                if os.path.exists(zip_file):
                    try:
                        os.unlink(zip_file)
                        print(f"Removed local release ZIP: {os.path.basename(zip_file)}")
                    except Exception as e:
                        print(f"Failed to remove {zip_file}: {e}")

if __name__ == "__main__":
    main()
