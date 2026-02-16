### Examine

gas-turbine-examine-stator-null = Кажется, здесь не хватает статора.
gas-turbine-examine-stator = Статор на месте.
gas-turbine-examine-blade-null = Кажется, здесь не хватает лопасти турбины.
gas-turbine-examine-blade = Лопасть турбины на месте.
gas-turbine-spinning-0 = Лопасти не вращаются.
gas-turbine-spinning-1 = Лопасти вращаются медленно.
gas-turbine-spinning-2 = Лопасти вращаются.
gas-turbine-spinning-3 = Лопасти вращаются быстро.
gas-turbine-spinning-4 = [color=red]Лопасти вращаются бесконтрольно![/color]
gas-turbine-damaged-0 = Устройство выглядит исправным.
gas-turbine-damaged-1 = Турбина выглядит слегка потрёпанной.
gas-turbine-damaged-2 = [color=yellow]Турбина выглядит сильно повреждённой.[/color]
gas-turbine-damaged-3 = [color=orange]Критические повреждения![/color]
gas-turbine-ruined = [color=red]Полностью разрушена![/color]

### Popups

# Shown when an event occurs
gas-turbine-overheat = { $owner } активирует аварийный клапан сброса перегрева!
gas-turbine-explode = { CAPITALIZE($owner) } разлетается на куски!
# Shown when damage occurs
gas-turbine-spark = { CAPITALIZE($owner) } начинает искрить!
gas-turbine-spark-stop = { CAPITALIZE($owner) } перестаёт искрить.
gas-turbine-smoke = { CAPITALIZE($owner) } начинает дымить!
gas-turbine-smoke-stop = { CAPITALIZE($owner) } перестаёт дымить.
# Shown during repairs
gas-turbine-repair-fail-blade = Вам нужно заменить лопасть турбины, прежде чем приступать к ремонту.
gas-turbine-repair-fail-stator = Вам нужно заменить статор, прежде чем приступать к ремонту.
gas-turbine-repair-ruined = Вы чините корпус { $target } с помощью { $tool }.
gas-turbine-repair-partial = Вы частично восстанавливаете повреждения { $target } с помощью { $tool }.
gas-turbine-repair-complete = Вы заканчиваете ремонт { $target } с помощью { $tool }.
gas-turbine-repair-no-damage = На { $target } нет повреждений, которые можно было бы исправить с помощью { $tool }.
# Anchoring warnings
gas-turbine-unanchor-warning = Вы не можете открутить { $owner }, пока турбина вращается!
gas-turbine-anchor-warning = Неверное положение для закрепления.
gas-turbine-eject-fail-speed = Вы не можете извлекать детали, пока турбина вращается!
gas-turbine-insert-fail-speed = Вы не можете вставлять детали, пока турбина вращается!

### UI

# Shown when using the UI
gas-turbine-ui-tab-main = Управление
gas-turbine-ui-tab-parts = Детали
gas-turbine-ui-rpm = ОБ/МИН
gas-turbine-ui-overspeed = ПЕРЕГРУЗКА
gas-turbine-ui-overtemp = ПЕРЕГРЕВ
gas-turbine-ui-stalling = СТОПОР
gas-turbine-ui-undertemp = ОХЛАЖДЕНИЕ
gas-turbine-ui-flow-rate = Скорость потока
gas-turbine-ui-stator-load = Нагрузка статора
gas-turbine-ui-blade = Лопасть турбины
gas-turbine-ui-blade-integrity = Целостность
gas-turbine-ui-blade-stress = Износ
gas-turbine-ui-stator = Статор турбины
gas-turbine-ui-stator-potential = Потенциал
gas-turbine-ui-stator-supply = Выработка
gas-turbine-ui-power = { POWERWATTS($power) }
gas-turbine-ui-locked-message = Управление заблокировано.
gas-turbine-ui-footer-left = Опасно: быстро движущиеся механизмы.
gas-turbine-ui-footer-right = 2.1 REV 1
