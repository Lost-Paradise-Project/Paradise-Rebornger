player-uuid-requirement =
    Это { $inverted ->
        [true] { " " } не
       *[other] { "" }
    } принадлежит вам
player-sponsortier-requirement =
    Вы должны обладать подпиской { $inverted ->
        [true] { " " } не
       *[other] { "" }
    } выше { $tier } уровня
player-sponsor-job-fail = Недостаточный уровень подписки. Требуется 4-й уровень
