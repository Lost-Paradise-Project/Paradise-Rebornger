player-uuid-requirement = It do {$inverted ->
    [true]{" "} not
    *[other]{""}
} belong to you
player-sponsortier-requirement = You must have a subscription {$inverted ->
    [true]{" "} lower
    *[other]{""} higher
} than {$tier} level
player-sponsor-job-fail = Insufficient subscription level. Level 4 required.
