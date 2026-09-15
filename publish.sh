#!/usr/bin/bash

dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r win-x64
