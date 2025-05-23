pip install -r requirements.txt
cd agent
pip install -e .
cd ..
export PYTHONPATH="$PYTHONPATH:$(pwd)/env"
export PYTHONPATH="$PYTHONPATH:$(pwd)"
export PYTHONPATH="$PYTHONPATH:$(pwd)/agent/stardojo"
export PYTHONPATH="$PYTHONPATH:$(pwd)/agent"
cd env