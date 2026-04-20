entity-effect-guidebook-speak-dataset =
    { $chance ->
        [1] Заставит
        *[other] заставляет
    } произнести случайное слово

entity-effect-guidebook-cure-disease-lower-stage =
    { $chance ->
        [1] Снизит
        *[other] снижает
    } стадию болезни { $disease }

entity-effect-guidebook-transition-disease =
    { $chance ->
        [1] Вызовет
        *[other] вызывает
    } переход болезни из { $fromDisease } в { $toDisease }

entity-effect-guidebook-cause-disease =
    { $chance ->
        [1] Заразит
        *[other] заражает
    } цель болезнью { $disease }
