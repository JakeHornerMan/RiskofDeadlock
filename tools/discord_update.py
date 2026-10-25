"""Posts "Risk of Deadlock <version> is out on Melty" (or on Thunderstore) with that version's CHANGELOG section to the Discord
#update channel (creator, 2026-10-24: "post here everytime there is a updtae pushed to melty"). Run it right after every Melty
`publish`; for Thunderstore, the watcher below posts once the moderators approve the version (creator, 2026-10-25).

    python tools/discord_update.py 1.0.0            # post "is out on Melty"
    python tools/discord_update.py 1.0.0 --dry-run  # print the message only
    python tools/discord_update.py 1.0.0 --thunderstore            # "is out on Thunderstore" instead
    python tools/discord_update.py 1.0.0 --thunderstore --if-live  # post only once Thunderstore approved this version (the
        API's review_status) and only once (a marker file). GitHub runs it every 3 hours (.github/workflows/thunderstore-watch.yml,
        with the webhook as the repo secret DISCORD_UPDATE_WEBHOOK) and switches the workflow off after posting
    python tools/discord_update.py --ts-latest   # print the latest version Thunderstore has (for the workflow)

Any other flag is refused, so a typo never posts by accident (2026-10-25: a half-applied edit ignored --if-live and posted).
The channel's webhook link is a secret: it lives only in %LOCALAPPDATA%\\DeadlockOfRain\\discord-update-webhook.txt (or the
DISCORD_UPDATE_WEBHOOK environment variable), never in the repo, logs or replies, and this script never prints it."""
import json, os, re, subprocess, sys, urllib.request

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
MELTY = "https://melty.gg/m/risk-of-deadlock-2"
TS_PAGE = "https://thunderstore.io/c/riskofrain2/p/JakeHornerMan/Risk_of_Deadlock/"
TS_API = "https://thunderstore.io/api/experimental/package/JakeHornerMan/Risk_of_Deadlock/"
GUIDE = "https://github.com/JakeHornerMan/RiskofDeadlock/blob/main/INSTALL.md"
DATA = os.path.join(os.environ.get("LOCALAPPDATA") or os.environ.get("RUNNER_TEMP") or ".", "DeadlockOfRain")
FLAGS = {"--dry-run", "--thunderstore", "--if-live", "--ts-latest"}
LIMIT = 2000   # Discord's message length


def section(version):
    text = open(os.path.join(ROOT, "thunderstore", "CHANGELOG.md"), encoding="utf-8").read()
    m = re.search(r"^## " + re.escape(version) + r"\s*$(.*?)(?=^## |\Z)", text, re.M | re.S)
    if not m: sys.exit(f"thunderstore/CHANGELOG.md has no '## {version}' section")
    return [l for l in m.group(1).strip().splitlines() if l.strip()]


def message(version, thunderstore):
    lines = [l for l in section(version) if not l.lstrip("- ").startswith("Built with")]
    head = f"**Risk of Deadlock {version} is out on {'Thunderstore' if thunderstore else 'Melty'}**"
    foot = (f"\nGet it: {TS_PAGE} (r2modman: search \"Risk of Deadlock\")\nInstall guide: {GUIDE}" if thunderstore
            else f"\nGet it: {MELTY}")
    body = "\n".join(lines)
    room = LIMIT - len(head) - len(foot) - 2
    if len(body) > room: body = body[:room - 1].rsplit("\n", 1)[0] + "\n…"
    return f"{head}\n{body}{foot}"


def webhook():
    url = os.environ.get("DISCORD_UPDATE_WEBHOOK")
    if not url:
        path = os.path.join(DATA, "discord-update-webhook.txt")
        if not os.path.exists(path): sys.exit(f"no webhook: {path} is missing (Discord #update > Integrations > Webhooks > Copy Webhook URL)")
        url = open(path, encoding="ascii").read().strip()
    if not re.match(r"^https://(discord|discordapp)\.com/api/webhooks/\d+/[\w-]+$", url): sys.exit("the webhook file does not hold a Discord webhook link")
    return url


def ts_package():
    req = urllib.request.Request(TS_API, headers={"User-Agent": "RiskOfDeadlock-update/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)


def ts_live(version):
    """Live = Thunderstore's moderators approved the package for Risk of Rain 2 and this version is the latest."""
    d = ts_package()
    status = next((c.get("review_status") for c in d.get("community_listings", []) if c.get("community") == "riskofrain2"), None)
    latest = d.get("latest", {}).get("version_number")
    print(f"Thunderstore: latest {latest}, review status {status}")
    return latest == version and status == "approved"


def remove_task(version):
    """A local watcher task (Windows) is done once the post went out; on GitHub the workflow switches itself off instead."""
    if os.name != "nt": return
    subprocess.run(["schtasks", "/Delete", "/TN", f"RiskOfDeadlock-Thunderstore-{version}", "/F"], capture_output=True)


def main():
    flags = {a for a in sys.argv[1:] if a.startswith("--")}
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if flags == {"--ts-latest"} and not args:
        print(ts_package().get("latest", {}).get("version_number", "")); return
    if len(args) != 1 or flags - FLAGS: sys.exit(__doc__)
    ts = "--thunderstore" in flags
    if "--if-live" in flags and not ts: sys.exit("--if-live is for --thunderstore")
    text = message(args[0], ts)
    if "--dry-run" in flags:
        print(text); return
    marker = os.path.join(DATA, f"posted-thunderstore-{args[0]}.txt")
    if "--if-live" in flags:
        if os.path.exists(marker): print("already posted"); remove_task(args[0]); return
        if not ts_live(args[0]): return
    body = json.dumps({"username": "Risk of Deadlock", "content": text, "allowed_mentions": {"parse": []}}).encode()
    req = urllib.request.Request(webhook() + "?wait=true", data=body, method="POST",
                                 headers={"Content-Type": "application/json", "User-Agent": "RiskOfDeadlock-update/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        print(f"posted to #update (HTTP {r.status}, message {json.load(r).get('id')})")
    if ts:
        os.makedirs(DATA, exist_ok=True)
        open(marker, "w").write("posted\n")
        if "--if-live" in flags: remove_task(args[0])


if __name__ == "__main__":
    main()
