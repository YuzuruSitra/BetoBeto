"""Serve the WebGL build locally: python Tools/serve_webgl.py [--port 8090]."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--port', type=int, default=8090)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1] / 'Builds' / 'WebGL'
if not (root / 'index.html').is_file():
    parser.error('Builds/WebGL/index.html is missing. Build using BetoBeto > Build > WebGL in Unity.')
handler = partial(SimpleHTTPRequestHandler, directory=str(root))
server = ThreadingHTTPServer(('127.0.0.1', args.port), handler)
print(f'BetoBeto: http://localhost:{args.port}  (Ctrl+C to stop)', flush=True)
try:
    server.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    server.server_close()
