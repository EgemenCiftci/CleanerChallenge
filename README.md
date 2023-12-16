# Cleaner Challenge
Basic robot vacuum cleaner algorithm in C#.
- Can go up to 224 different directions
- Decides direction based on dustiness (greedy)
- Does not pull socks most of the time due to high penalty
- Keeps history of target positions to not get stuck between the same position pattern
- If it gets stuck somehow, it recovers itself by trying random directions
