-markings-selection =
    { $selectable ->
        [0] У вас не осталось доступных маркировок.
        [one] Вы можете выбрать ещё одну маркировку.
       *[other] Вы можете выбрать ещё { $selectable } маркировок.
    }
markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите хотя бы одну маркировку.
        [0] Вы не можете выбрать ни одной маркировки, но почему-то должны? Это ошибка.
        [one] Выберите одну маркировку.
       *[other] Выберите хотя бы одну и до {$count} маркировок. { -markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Выберите любое количество маркировок.
        [0] Вы не можете выбрать ни одной маркировки.
        [one] Выберите не более одной маркировки.
       *[other] Выберите до {$count} маркировок. { -markings-selection(selectable: $selectable) }
    }
}
markings-reorder = Изменить порядок маркировок
humanoid-marking-modifier-respect-limits = Соблюдать лимиты
humanoid-marking-modifier-respect-group-sex = Соблюдать ограничения по группе и полу
markings-organ-Torso = Торс
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая ступня
markings-organ-FootRight = Правая ступня
markings-organ-Eyes = Глаза
markings-layer-Special = Особые
markings-layer-Tail = Хвост
markings-layer-Tail-Moth = Крылья
markings-layer-Hair = Волосы
markings-layer-FacialHair = Растительность на лице
markings-layer-Chest = Грудь
markings-layer-Head = Голова
markings-layer-Snout = Морда
markings-layer-SnoutCover = Морда (Покров)
markings-layer-HeadSide = Голова (Сбоку)
markings-layer-HeadTop = Голова (Сверху)
markings-layer-Eyes = Глаза
markings-layer-RArm = Правая рука
markings-layer-LArm = Левая рука
markings-layer-RHand = Правая кисть
markings-layer-LHand = Левая кисть
markings-layer-RLeg = Правая нога
markings-layer-LLeg = Левая нога
markings-layer-RFoot = Правая ступня
markings-layer-LFoot = Левая ступня
markings-layer-Overlay = Наложение
markings-search = Поиск
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:
