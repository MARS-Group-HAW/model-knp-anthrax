#!/usr/bin/env bash

models=(
  "KNPAnthrax" 
)

# Use ./build.sh <modelname> to only build one project
if [ -n "$1" ]
  then
    models=("$1")
fi

for val in "${models[@]}"; do
    echo "Build box for model ${val}" 
    output=$val
    
    cd "$val" || exit 
    rm -rf "$val"
    dotnet publish -c Release -r osx-x64 /p:DebugSymbols=false /p:DebugType=None \
      --self-contained true -o "${output}"/"${output}"_MACOSX && \
      cp Resources/ ./"${output}"/"${output}"_MACOSX/Resources && \
      cp config.json ./"${output}"/"${output}"_MACOSX/
    
    dotnet publish -c Release -r win-x64 /p:DebugSymbols=false /p:DebugType=None \
      --self-contained true -o "${output}"/"${output}"_WINDOWS && \
      cp Resources/ ./"${output}"/"${output}"_WINDOWS/Resources && \
      cp config.json ./"${output}"/"${output}"_WINDOWS/
      
    dotnet publish -c Release -r linux-x64 /p:DebugSymbols=false /p:DebugType=None \
      --self-contained true -o "${output}"/"${output}"_LINUX && \
      cp Resources/ ./"${output}"/"${output}"_LINUX/Resources && \
      cp config.json ./"${output}"/"${output}"_LINUX/
    
    zip -r ../"${output}"_WINDOWS.zip ./"${output}"/"${output}"_WINDOWS/
    zip -r ../"${output}"_MACOSX.zip ./"${output}"/"${output}"_MACOSX/
    zip -r ../"${output}"_LINUX.zip ./"${output}"/"${output}"_LINUX/
    
    cd ..
done