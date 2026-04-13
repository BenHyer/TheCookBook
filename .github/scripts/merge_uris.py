import json, sys

current = json.loads(sys.argv[1])
new = [sys.argv[2], sys.argv[3]]
merged = sorted(set(current + new))
print(json.dumps({"web": {"redirectUris": merged}}))
