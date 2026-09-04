disk-decryptor-disk-slot = слот для диска
disk-decryptor-processor-slot = слот для процессора

disk-decryptor-title = Дешифратор дисков
disk-decryptor-integrity-label = Целостность
disk-decryptor-time-label = Время
disk-decryptor-start-manual = Начать вручную
disk-decryptor-start-auto = Авто-дешифровка
disk-decryptor-claim = Прервать и забрать очки
disk-decryptor-lock = Зафиксировать

disk-decryptor-tier-badge = ТИР { $tier }

disk-decryptor-calibration-hint = Нажмите «Зафиксировать», когда маркер окажется в зелёной зоне.
disk-decryptor-circuit-hint = Проложите путь от синего узла к зелёному, не заходя в заблокированные (красные).
disk-decryptor-lights-hint = Кликайте по ячейкам, чтобы погасить все огни. Клик переключает и соседние ячейки.
disk-decryptor-memory-hint = Найдите все пары одинаковых карт.
disk-decryptor-breach-hint = 
    Соберите показанную последовательность кодов.
    Первый код берётся из любой ячейки верхней строки. 
    Каждый следующий код — по очереди: сначала из того же столбца, что и предыдущий выбор, потом из той же строки, и так далее по кругу.
disk-decryptor-pipe-hint = 
    Кликайте по сегментам трубы, чтобы повернуть их на 90°.
    Соберите сплошной поток от начального сегмента (жёлтый фон) до конечного.
disk-decryptor-flow-hint = 
    Проложите маршрут по сети узлов и активируйте контрольные точки строго по порядку (1, 2, 3...), последняя точка — цель взлома.
    Клик по соседнему узлу — шаг вперёд, клик по предыдущему — шаг назад.

disk-decryptor-circuit-moves = Осталось ходов: { $moves }
disk-decryptor-lights-remaining = Горит огней: { $count }
disk-decryptor-memory-found = Найдено пар: { $found } / { $total }
disk-decryptor-breach-buffer = Буфер: { $moves }
disk-decryptor-pipe-broken = Поток разорван
disk-decryptor-pipe-connected = Поток подключён!
disk-decryptor-flow-progress = Точек активировано: { $done } / { $total }
disk-decryptor-pattern-hint = 
    Запомните подсвеченные ячейки.
    Когда подсказка погаснет, кликните ровно те же ячейки.
disk-decryptor-pattern-progress = Найдено: { $found } / { $total }
disk-decryptor-code-hint = 
    Кликайте по ячейкам, чтобы менять цвет, затем нажмите «Отправить».
    Обратная связь покажет точные и частичные совпадения.
disk-decryptor-code-attempts = Попыток осталось: { $left }
disk-decryptor-code-no-feedback = Пока нет данных
disk-decryptor-code-feedback = Точно: { $exact }, частично: { $partial }
disk-decryptor-code-submit = Отправить
disk-decryptor-jam-hint = Кликайте по подсвеченной ячейке, пока она не погасла.
disk-decryptor-jam-progress = Поймано: { $done } / { $total }
disk-decryptor-bands-hint = Зафиксируйте каждую полосу отдельно, когда маркер окажется в зелёной зоне.
disk-decryptor-bands-progress = Зафиксировано: { $done } / { $total }
disk-decryptor-sweep-hint = 
    Раскрывайте безопасные ячейки.
    Число показывает, сколько ловушек рядом. Не попадите на ловушку.
disk-decryptor-sweep-progress = Раскрыто ячеек: { $count }
disk-decryptor-sweep-flag-off = Пометить мину
disk-decryptor-sweep-flag-on = Режим пометки (вкл)

disk-decryptor-status-idle = Ожидание диска.
disk-decryptor-status-calibration = Калибровка частоты. Слой { $layer } / { $total }
disk-decryptor-status-circuit = Трассировка цепи. Слой { $layer } / { $total }
disk-decryptor-status-lightsout = Гашение сигналов. Слой { $layer } / { $total }
disk-decryptor-status-memory = Сопоставление данных. Слой { $layer } / { $total }
disk-decryptor-status-breach = Взлом протокола. Слой { $layer } / { $total }
disk-decryptor-status-pipe = Перенаправление потока. Слой { $layer } / { $total }
disk-decryptor-status-flow = Взлом сети. Слой { $layer } / { $total }
disk-decryptor-status-pattern = Восстановление паттерна. Слой { $layer } / { $total }
disk-decryptor-status-code = Подбор кода. Слой { $layer } / { $total }
disk-decryptor-status-jam = Перехват сигнала. Слой { $layer } / { $total }
disk-decryptor-status-bands = Настройка частот. Слой { $layer } / { $total }
disk-decryptor-status-sweep = Разминирование сектора. Слой { $layer } / { $total }
disk-decryptor-status-auto = Идёт автоматическая дешифровка...

disk-decryptor-recipe-none = Рецепт: неизвестно
disk-decryptor-recipe-hint = Рецепт: { $hint }

disk-decryptor-no-server = Аппарат не подключён к серверу исследований.
disk-decryptor-claimed = Вы прервали дешифровку и получили очки исследования.
disk-decryptor-success = Дешифровка завершена! Разблокирован рецепт: { $recipe }
disk-decryptor-fail = Целостность диска нарушена. Диск повреждён и уничтожен.

encrypted-tech-disk-examine = Диск зашифрован. Нужен специальный дешифратор, чтобы понять, что на нём записано.

disk-decryptor-recipe-unknown = неизвестный рецепт
