### Popups

reactor-smoke-start = { CAPITALIZE($owner) } начинает дымиться!
reactor-smoke-stop = { CAPITALIZE($owner) } перестаёт дымиться.
reactor-fire-start = { CAPITALIZE($owner) } начинает гореть!
reactor-fire-stop = { CAPITALIZE($owner) } перестаёт гореть.
reactor-unanchor-melted = Вы не можете открутить ядерный реактор, он вплавился в корпус!
reactor-unanchor-warning = Вы не можете открутить ядерный реактор, пока он не пуст или его температура выше 80°C!
reactor-anchor-warning = Неверное положение для закрепления.

### Messages

reactor-smoke-start-message = ВНИМАНИЕ: { CAPITALIZE($owner) } достиг опасной температуры: { $temperature }K. Немедленно вмешайтесь, чтобы предотвратить расплавление.
reactor-smoke-stop-message = Температура { $owner } опустилась ниже опасного уровня. Хорошего дня.
reactor-fire-start-message = ВНИМАНИЕ: { CAPITALIZE($owner) } достиг КРИТИЧЕСКОЙ температуры: { $temperature }K. РАСПЛАВЛЕНИЕ НЕИЗБЕЖНО.
reactor-fire-stop-message = Температура { $owner } опустилась ниже критического уровня. Расплавление предотвращено.
reactor-temperature-dangerous-message = { CAPITALIZE($owner) } имеет опасную температуру: { $temperature }K.
reactor-temperature-critical-message = { CAPITALIZE($owner) } имеет критическую температуру: { $temperature }K.
reactor-temperature-cooling-message = { CAPITALIZE($owner) } охлаждается: { $temperature }K.
reactor-melting-announcement = Ядерный реактор на борту станции начинает плавиться. Рекомендуется эвакуация из прилегающих зон.
reactor-melting-announcement-sender = Ядерная угроза
reactor-meltdown-announcement = На ядерном реакторе станции произошла катастрофическая перегрузка. Вероятны радиоактивные обломки, выпадение осадков и возгорание хладагента. Настоятельно рекомендуется немедленная эвакуация из прилегающих зон.
reactor-meltdown-announcement-sender = Расплавление реактора

### UI

comp-nuclear-reactor-ui-locked = Заблокировано
comp-nuclear-reactor-ui-insert-button = Вставить
comp-nuclear-reactor-ui-remove-button = Убрать
comp-nuclear-reactor-ui-eject-button = Извлечь
comp-nuclear-reactor-ui-view-change = Сменить вид
comp-nuclear-reactor-ui-view-temp = Температура
comp-nuclear-reactor-ui-view-neutron = Нейтроны
comp-nuclear-reactor-ui-view-target = Цель
comp-nuclear-reactor-ui-view-fuel = Топливо
comp-nuclear-reactor-ui-status-panel = Статус реактора
comp-nuclear-reactor-ui-reactor-temp = Температура
comp-nuclear-reactor-ui-reactor-rads = Радиация
comp-nuclear-reactor-ui-reactor-therm = Тепловая мощность
comp-nuclear-reactor-ui-reactor-control = Стержни управления
comp-nuclear-reactor-ui-therm-format = { POWERWATTS($power) }т
comp-nuclear-reactor-ui-footer-left = Опасно: высокий уровень радиации.
comp-nuclear-reactor-ui-footer-right = 0.8 REV 3
