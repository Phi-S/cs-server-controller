#!/usr/bin/env python
# with help from https://stackoverflow.com/a/31968411/3592326
import os
import pty
import sys
import subprocess

output = ''
command = sys.argv[1]
master, slave = pty.openpty()
p = subprocess.Popen(command.split(), stdout=slave)
os.close(slave)

while True:
    try:
        # read in a chunk of data
        data = os.read(master, 1024)
        decodedData = data.decode('ascii')
        sys.stdout.write(str(decodedData))
        sys.stdout.flush()
    except OSError as e:
        break
