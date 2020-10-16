@echo off
"../src/Cli/gx-md/bin/Debug/netcoreapp3.1/gx-md" --log-level=trace create --metadata="./tqae/base-0.g.gxmd ; ./tqae/base-1.g.gxmp " --output=./gxm-tqae.gxmd --output-format=gxmd
