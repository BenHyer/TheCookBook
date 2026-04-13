import json, sys

current = json.loads(sys.argv[1])
pr_num = sys.argv[2]
filtered = [u for u in current if f"pr-{pr_num}." not in u]
print(json.dumps({"web": {"redirectUris": filtered}}))
