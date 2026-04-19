entity-effect-guidebook-speak-dataset =
    { $chance ->
        [1] Will force
        *[other] force
    } speak a random word

entity-effect-guidebook-cure-disease-lower-stage =
    { $chance ->
        [1] Lowers
        *[other] Lower
    } stage of the disease { $disease }

entity-effect-guidebook-transition-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } disease transition from { $fromDisease } to { $toDisease }

entity-effect-guidebook-cause-disease =
    { $chance ->
        [1] Causes
        *[other] cause
    } infects the target with disease { $disease }
