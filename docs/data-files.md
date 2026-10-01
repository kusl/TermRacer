# Data files

All files are UTF-8 text with `\n` line endings, tab-separated, with numbers in invariant culture and timestamps in UTC. Lines starting with `#` are comments. The track id (for example `gullwing-park-5e8f8786`) is the circuit name plus a hash of its layout; rows and files for another id are kept but ignored.

## laps.tsv

One row per completed lap, appended as soon as the lap ends.

```
# TermRacer lap log. One lap per row, tab separated, times in seconds, timestamps in UTC.
recorded_utc	track	mode	lap	driver	seconds	wall_hits	off_track_seconds	replay
2026-10-01T22:51:03.123Z	gullwing-park-5e8f8786	single	1	manual	56.409	0	0.00	20261001-225103123-single-1
```

| Column | Meaning |
| --- | --- |
| `recorded_utc` | When the lap finished |
| `track` | Track id |
| `mode` | `single` or `zen` |
| `lap` | Lap number within the session; lap 1 is a standing start, later laps are flying laps |
| `driver` | `manual`, `autopilot`, or `mixed` when the wheel changed hands during the lap |
| `seconds` | Lap time |
| `wall_hits` | Wall impacts during the lap |
| `off_track_seconds` | Time spent on the grass |
| `replay` | Replay name in `replays/`, or `-` when none was saved |

Columns are read by name, so they may be reordered or extended. When the header differs from the current one, or rows cannot be read, the file is rewritten in the current layout on the next start and unreadable rows are kept as `# unreadable:` comments.

## replays/*.tsv.gz

One gzip-compressed text file per saved lap, named after the `replay` column. View one with `zcat` or `gzip -dc`.

```
# TermRacer replay. Header fields, then one sample per row at the given rate (samples per second).
track	gullwing-park-5e8f8786
recorded_utc	2026-10-01T22:51:03.123Z
mode	single
lap	1
driver	manual
seconds	56.40892681710728
rate	30
samples	1693
t	x	y	heading	speed	distance	throttle	brake	steer	flags
0.000	0.00	-4.56	-0.1322	17.67	0.00	1	0	0.433	A
```

Samples start on the line at `t` 0 and cover the whole lap. `x` and `y` are metres, `heading` radians (world y points down), `speed` metres per second along the heading (negative when reversing), `distance` metres along the centreline since the line, `throttle` and `brake` 0 to 1, `steer` −1 (left) to 1 (right). `flags` holds `A` while the autopilot drove and `G` while on the grass, or `-`. A lap is about 30 KB.

The fastest five laps of each start type, the fastest five manual laps of each start type, and the 20 most recent laps keep their replays; the rest are deleted after each lap.

## autopilot.tsv

The autopilot's training, rewritten after every lap it drives on its own.

```
# TermRacer autopilot training. One speed factor per corner: safe is proven, limit is the lowest that went wrong, trial is the next attempt.
track	gullwing-park-5e8f8786
laps	14
corner	start	length	safe	limit	trial
0	189	20	1.19	1.195	1.19
```

`start` and `length` locate the corner in centreline points (2 m apart). Factors scale the grip the speed profile plans with; `limit` is `inf` until something went wrong. If the corners no longer match the track, or the file is damaged, training starts again from scratch. Delete the file to retrain.
