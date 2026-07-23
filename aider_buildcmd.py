#!/usr/bin/env python3
"""
Simple build helper script for the EfCoreNPlusOneGuard repository.

Running this script will execute `dotnet test` in the repository root,
allowing you to run all unit tests (including the newly added
NPlusOneIncidentJsonExtensionsJsonExtensionsTests) from a single command:

    python3 ./aider_buildcmd.py
"""

import subprocess
import sys
from pathlib import Path

def main() -> int:
    # Determine the repository root (the directory containing this script)
    repo_root = Path(__file__).resolve().parent

    # Execute `dotnet test` in the repository root
    try:
        result = subprocess.run(
            ["dotnet", "test"],
            cwd=repo_root,
            check=False,
        )
    except FileNotFoundError:
        print("Error: 'dotnet' executable not found. Ensure the .NET SDK is installed and on PATH.", file=sys.stderr)
        return 1

    return result.returncode

if __name__ == "__main__":
    sys.exit(main())
