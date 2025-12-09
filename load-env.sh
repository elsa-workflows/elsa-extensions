#!/bin/bash

# Script to load environment variables from .env.local
# Usage: source load-env.sh

if [ -f ".env.local" ]; then
    echo "Loading environment variables from .env.local..."
    export $(grep -v '^#' .env.local | xargs)
    echo "✅ Environment variables loaded successfully"
    echo "OPENAI_API_KEY is now set (length: ${#OPENAI_API_KEY} characters)"
else
    echo "❌ .env.local file not found"
    echo "Create it with your OpenAI API key:"
    echo "echo 'OPENAI_API_KEY=your-key-here' > .env.local"
fi