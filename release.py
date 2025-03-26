#! python

from subprocess import call

import subprocess
import fileinput
import re
import os
import sys
import getopt

origPath = os.getcwd();

def safeProcess( cmd ):
    "Run command, return boolean for success"
    print(cmd);
    try:
        out = subprocess.check_output(cmd, shell=True)
        print(out.decode("utf-8").replace("\\\n", "\n"))
        return True;
    except subprocess.CalledProcessError as e:                                                                                                   
        print("Status : FAIL", e.returncode, e.output)
        return False;
        
def safeExit():
    print ("Exiting...")
    os.chdir(origPath)
    sys.exit(1)
#    safeProcess("git reset --hard head")

def get_version(filename):
    pattern = re.compile(r"\[assembly:\s*AssemblyVersion\(\"(\d*.\d*.\d*)\"\)\]")

    for line in fileinput.input(filename):
        if re.search(pattern, line): 
            version = pattern.search(line).groups()[0]
            return version
    return None;        
  
# Don't continue if working copy is dirty
if not safeProcess('git diff-index --quiet HEAD --'):
    print( "Cannot build, git working copy dirty")
    safeExit()
    
# Build all projects
# FYI if you're running this directly in git bash, you need to escape the forward slashes in the options (e.g. //Rebuild)
clean = 'devenv.com OptiKeyDeployment.sln /Clean "Release|Any CPU"'
if not safeProcess(clean):
    print("Error cleaning project")
    safeExit()

build = 'devenv.com OptiKeyDeployment.sln /Rebuild "Release|Any CPU"'
if not safeProcess(build):
    print("Error building project")
    safeExit()

# Build installer 
installer_file = r"installer\EyeMine.aip"
if not safeProcess("AdvancedInstaller.com /rebuild {}".format(installer_file)):
    print("Failed building installer")
    safeExit()

# Discard local changes to InstallerStrings (these are a build artefact)
if not safeProcess("git checkout src/JuliusSweetland.OptiKey.InstallerActions/InstallerStrings.cs"):
    print("Error checking out InstallerStrings.cs")
    safeExit()

# Tag code by version
version_file = 'src/JuliusSweetland.OptiKey.EyeMine/Properties/AssemblyInfo.cs'
version = get_version(version_file)
safeProcess("git tag release/eyemine_exhibit_{}".format(version))



