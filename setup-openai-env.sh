#!/bin/bash

# Script to set up OpenAI API key environment variable
# Usage: source setup-openai-env.sh

echo "Setting up OpenAI API key environment variable..."
echo "Please enter your OpenAI API key (it will be hidden):"
read -s OPENAI_API_KEY

if [ -z "$OPENAI_API_KEY" ]; then
    echo "No API key provided. Exiting..."
    return 1 2>/dev/null || exit 1
fi

export OPENAI_API_KEY="$OPENAI_API_KEY"

echo "OpenAI API key has been set as environment variable OPENAI_API_KEY"
echo "You can now run the tests with: dotnet test test/unit/Elsa.OpenAI.Tests/"
echo ""
echo "To make this persistent across shell sessions, add this to your ~/.zshrc:"
echo "export OPENAI_API_KEY=\"your-api-key-here\""