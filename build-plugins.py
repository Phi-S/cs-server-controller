import os
import shutil
import subprocess


def main():
    print("Building all server plugins")
    file_dir = os.path.dirname(os.path.realpath(__file__))
    instance_folder = os.path.join(file_dir, "src", "instance")
    plugins_src_folder = os.path.join(instance_folder, "plugins")
    plugins_dest_folder = os.path.join(instance_folder, "Application", "ServerPluginsFolder", "plugins")

    shutil.rmtree(plugins_dest_folder)
    plugins = os.listdir(plugins_src_folder)
    for plugin in plugins:
        if plugin == "SharedPluginLib":
            continue

        print(f"Building {plugin}")
        plugin_project_path = os.path.join(plugins_src_folder, plugin, f"{plugin}.csproj")
        plugin_dest_path = os.path.join(plugins_dest_folder, plugin)
        run_command(["dotnet", "build", plugin_project_path, "-o", plugin_dest_path])


# ==========================

def run_command(command: list[str]):
    command_as_string = " ".join(command)
    print(f"Executing command \"{command_as_string}\"")

    dir_path = os.path.dirname(os.path.realpath(__file__))
    p = subprocess.Popen(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        universal_newlines=True,
        cwd=dir_path)

    out = ""

    while p.poll() is None:
        line = p.stdout.readline()  # This blocks until it receives a newline.
        if not line:
            continue
        print(line.rstrip())
        out += line

    p.communicate()

    if p.returncode != 0:
        raise Exception(f"Error while trying to execute command: \"{command_as_string}\"")

    return out


if __name__ == "__main__":
    main()
