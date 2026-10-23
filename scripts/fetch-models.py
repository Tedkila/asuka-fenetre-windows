"""Build-time only. Immutable OpenCV Zoo models, verified against Git LFS SHA-256."""
import hashlib
import json
from pathlib import Path
import urllib.request

root = Path(__file__).resolve().parents[1] / 'models'
manifest = json.loads((root / 'manifest.json').read_text())
for model in manifest['models']:
    target = root / model['name']
    def valid(path):
        return path.exists() and path.stat().st_size == model['size'] and hashlib.sha256(path.read_bytes()).hexdigest() == model['sha256']
    if valid(target):
        print('Verified:', target.name)
        continue
    url = f"https://media.githubusercontent.com/media/opencv/opencv_zoo/{manifest['commit']}/models/{model['directory']}/{model['name']}"
    temp = target.with_suffix('.download')
    try:
        with urllib.request.urlopen(url, timeout=120) as response, temp.open('wb') as output:
            while data := response.read(1024 * 1024):
                output.write(data)
        if not valid(temp):
            raise RuntimeError(f'Invalid model checksum: {target.name}')
        temp.replace(target)
        print('Downloaded and verified:', target.name)
    finally:
        temp.unlink(missing_ok=True)
