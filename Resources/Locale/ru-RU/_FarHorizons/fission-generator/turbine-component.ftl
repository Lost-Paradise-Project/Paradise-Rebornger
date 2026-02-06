### Examine

gas-turbine-examine-stator-null = Кажется, отсутствует статор.
gas-turbine-examine-stator = Статор установлен.
gas-turbine-examine-blade-null = Кажется, отсутствует лопасть турбины.
gas-turbine-examine-blade = Лопасть турбины установлена.
turbine-spinning-0 = Лопасти не вращаются.
turbine-spinning-1 = Лопасти медленно поворачиваются.
turbine-spinning-2 = Лопасти вращаются.
turbine-spinning-3 = Лопасти вращаются быстро.
turbine-spinning-4 = [color=red]Лопасти вращаются бесконтрольно![/color]
turbine-damaged-0 = Похоже, устройство в хорошем состоянии.
turbine-damaged-1 = Турбина выглядит немного поцарапанной.
turbine-damaged-2 = [color=yellow]Турбина выглядит сильно повреждённой.[/color]
turbine-damaged-3 = [color=orange]Критическое повреждение![/color]
turbine-ruined = [color=red]Полностью сломано![/color]

### Popups

turbine-overheat = { CAPITALIZE($owner) } активирует аварийный клапан сброса перегрева!
turbine-explode = { CAPITALIZE($owner) } разлетается на куски!
turbine-spark = { CAPITALIZE($owner) } начинает искрить!
turbine-spark-stop = { CAPITALIZE($owner) } перестаёт искрить.
turbine-smoke = { CAPITALIZE($owner) } начинает дымиться!
turbine-smoke-stop = { CAPITALIZE($owner) } перестаёт дымиться.
gas-turbine-repair-fail-blade = Вам нужно заменить лопасть турбины, прежде чем это можно будет отремонтировать.
gas-turbine-repair-fail-stator = Вам нужно заменить статор, прежде чем это можно будет отремонтировать.
turbine-repair-ruined = Вы восстанавливаете корпус { $target } с помощью { $tool }.
turbine-repair = Вы чините часть повреждений { $target } с помощью { $tool }.
turbine-no-damage = На { $target } нет повреждений, которые можно было бы исправить с помощью { $tool }.
turbine-show-damage = Прочность лопастей: { $health }, Макс. прочность: { $healthMax }.
turbine-unanchor-warning = Вы не можете открутить газовую турбину, пока лопасти вращаются!
turbine-anchor-warning = Неверное положение для закрепления.
gas-turbine-eject-fail-speed = Вы не можете извлекать детали турбины, пока она вращается!
gas-turbine-insert-fail-speed = Вы не можете вставлять детали турбины, пока она вращается!

### UI

comp-turbine-ui-tab-main = Управление
comp-turbine-ui-tab-parts = Детали
comp-turbine-ui-rpm = Об/мин
comp-turbine-ui-overspeed = ПЕРЕГРУЗКА СКОРОСТИ
comp-turbine-ui-overtemp = ПЕРЕГРЕВ
comp-turbine-ui-stalling = ОСТАНОВКА
comp-turbine-ui-undertemp = НИЗКАЯ ТЕМПЕРАТУРА
comp-turbine-ui-flow-rate = Скорость потока
comp-turbine-ui-stator-load = Нагрузка статора
comp-turbine-ui-blade = Лопасть турбины
comp-turbine-ui-blade-integrity = Целостность
comp-turbine-ui-blade-stress = Напряжение
comp-turbine-ui-stator = Статор турбины
comp-turbine-ui-stator-potential = Потенциал
comp-turbine-ui-stator-supply = Питание
comp-turbine-ui-power = { POWERWATTS($power) }
comp-turbine-ui-locked-message = Управление заблокировано.
comp-turbine-ui-footer-left = Опасно: быстродвижущиеся механизмы.
comp-turbine-ui-footer-right = 2.0 REV 1
