#!/bin/bash
# Sourced by the lab scripts (run_test, run_bench, run_coop_test, dev_run, lab_restore): they only ever close a copy of
# Risk of Rain 2 they started themselves. On 2026-10-24 a test closed the game the creator had open, and Melty filed it as
# players' bugs ("closed right after starting", "its code didn't load"). The copies a script starts are recorded in
# $LOCALAPPDATA/DeadlockOfRain/lab-game-pids.txt; any other running copy is the creator's and is left alone.
# FORCE_CLOSE=1 closes every copy (the unattended daily watch at 04:00 sets it, doc 10).
GG_UM=/c/Users/jakeh/tools/universal-modder/bin/um
GG_OURS="$LOCALAPPDATA/DeadlockOfRain/lab-game-pids.txt"

game_pids() { powershell -NoProfile -Command "(Get-Process -Name \"Risk of Rain 2\" -ErrorAction SilentlyContinue).Id" | tr -d '\r'; }
game_is_ours() { [ -f "$GG_OURS" ] && grep -qx "$1" "$GG_OURS"; }

# before a script starts the game: stop if a copy we didn't start is running (exit 2), close our own leftovers
guard_open_game() {
  local p theirs=""
  for p in $(game_pids); do game_is_ours "$p" || [ -n "$FORCE_CLOSE" ] || theirs="$theirs $p"; done
  if [ -n "$theirs" ]; then
    echo "GAME_OPEN: Risk of Rain 2 is running and no lab script started it (the creator may be playing); left alone. Close it, or run with FORCE_CLOSE=1."
    exit 2
  fi
  close_our_games
}

# after a script started the game: every copy running now is ours (guard_open_game made sure none were running before)
record_our_game() {
  mkdir -p "$(dirname "$GG_OURS")"
  local p
  for p in $(game_pids); do game_is_ours "$p" || echo "$p" >>"$GG_OURS"; done
}

# close the copies we started (every copy with FORCE_CLOSE=1); a copy the creator started stays open
close_our_games() {
  local p left=""
  for p in $(game_pids); do
    if game_is_ours "$p" || [ -n "$FORCE_CLOSE" ]; then $GG_UM win kill "$p" >/dev/null; else left="$left $p"; fi
  done
  for i in $(seq 1 20); do
    local still=""
    for p in $(game_pids); do game_is_ours "$p" || [ -n "$FORCE_CLOSE" ] && still=1; done
    [ -z "$still" ] && break; sleep 1
  done
  rm -f "$GG_OURS"
  [ -n "$left" ] && echo "game left open (not started by a lab script):$left"
  return 0
}
