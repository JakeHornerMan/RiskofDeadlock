"""Posts "Risk of Deadlock <version> is out on Melty" with that version's CHANGELOG section to the Discord #update channel
(creator, 2026-10-24: "post here everytime there is a updtae pushed to melty"). Run it right after every Melty `publish`.

    python tools/discord_update.py 1.0.0            # post
    python tools/discord_update.py 1.0.0 --dry-run  # print the message only

The channel's webhook link is a secret: it lives only in %LOCALAPPDATA%\\DeadlockOfRain\\discord-update-webhook.txt (or the
DISCORD_UPDATE_WEBHOOK environment variable), never in the repo, logs or replies, and this script never prints it."""
import json, os, re, sys, urllib.request

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
MELTY = "https://melty.gg/m/risk-of-deadlock-2"
LIMIT = 2000   # Discord's message length


def section(version):
    text = open(os.path.join(ROOT, "thunderstore", "CHANGELOG.md"), encoding="utf-8").read()
    m = re.search(r"^## " + re.escape(version) + r"\s*$(.*?)(?=^## |\Z)", text, re.M | re.S)
    if not m: sys.exit(f"thunderstore/CHANGELOG.md has no '## {version}' section")
    return [l for l in m.group(1).strip().splitlines() if l.strip()]


def message(version):
    lines = [l for l in section(version) if not l.lstrip("- ").startswith("Built with")]
    head = f"**Risk of Deadlock {version} is out on Melty**"
    foot = f"\nGet it: {MELTY}"
    body = "\n".join(lines)
    room = LIMIT - len(head) - len(foot) - 2
    if len(body) > room: body = body[:room - 1].rsplit("\n", 1)[0] + "\n…"
    return f"{head}\n{body}{foot}"


def webhook():
    url = os.environ.get("DISCORD_UPDATE_WEBHOOK")
    if not url:
        path = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DeadlockOfRain", "discord-update-webhook.txt")
        if not os.path.exists(path): sys.exit(f"no webhook: {path} is missing (Discord #update > Integrations > Webhooks > Copy Webhook URL)")
        url = open(path, encoding="ascii").read().strip()
    if not re.match(r"^https://(discord|discordapp)\.com/api/webhooks/\d+/[\w-]+$", url): sys.exit("the webhook file does not hold a Discord webhook link")
    return url


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1: sys.exit(__doc__)
    text = message(args[0])
    if "--dry-run" in sys.argv:
        print(text); return
    body = json.dumps({"username": "Risk of Deadlock", "content": text, "allowed_mentions": {"parse": []}}).encode()
    req = urllib.request.Request(webhook() + "?wait=true", data=body, method="POST",
                                 headers={"Content-Type": "application/json", "User-Agent": "RiskOfDeadlock-update/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        print(f"posted to #update (HTTP {r.status}, message {json.load(r).get('id')})")


if __name__ == "__main__":
    main()
