-create-3rd-person =
    { $chance ->
        [1] Создаёт
       *[other] создают
    }
-cause-3rd-person =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    }
-satiate-3rd-person =
    { $chance ->
        [1] Утоляет
       *[other] утоляют
    }
entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $amount ->
        [1] { INDEFINITE($entname) }
       *[other] { $amount } { MAKEPLURAL($entname) }
    }
entity-effect-guidebook-destroy =
    { $chance ->
        [1] Уничтожает
       *[other] уничтожают
    } объект
entity-effect-guidebook-break =
    { $chance ->
        [1] Ломает
       *[other] ломают
    } объект
entity-effect-guidebook-explosion =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } взрыв
entity-effect-guidebook-emp =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } электромагнитный импульс
entity-effect-guidebook-flash =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } ослепляющую вспышку
entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество пены
entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество дыма
entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
       *[other] утоляют
    } жажду { $relative ->
        [1] в среднем темпе
       *[other] в { NATURALFIXED($relative, 3) } раза быстрее среднего темпа
    }
entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Утоляет
       *[other] утоляют
    } голод { $relative ->
        [1] в среднем темпе
       *[other] в { NATURALFIXED($relative, 3) } раза быстрее среднего темпа
    }
entity-effect-guidebook-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Исцеляет
                [deals] Наносит
               *[both] Изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] исцеляют
                [deals] наносят
               *[both] изменяют здоровье на
            }
    } { $changes }
entity-effect-guidebook-even-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Равномерно исцеляет
                [deals] Равномерно наносит
               *[both] Равномерно изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] равномерно исцеляют
                [deals] равномерно наносят
               *[both] равномерно изменяют здоровье на
            }
    } { $changes }
entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } с накоплением
        [set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
       *[remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } эффекта { LOC($key) }
    }
entity-effect-guidebook-status-effect =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } с накоплением
        [set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
       *[remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } эффекта { LOC($key) }
    } { $delay ->
        [0] немедленно
       *[other]
            после задержки в { NATURALFIXED($delay, 3) } { $delay ->
                [one] секунду
                [few] секунды
               *[other] секунд
            }
    }
entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } постоянный эффект { LOC($key) }
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } постоянный эффект { LOC($key) }
        [set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } постоянный эффект { LOC($key) }
       *[remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } эффект { LOC($key) }
    } { $delay ->
        [0] немедленно
       *[other]
            после задержки в { NATURALFIXED($delay, 3) } { $delay ->
                [one] секунду
                [few] секунды
               *[other] секунд
            }
    }
entity-effect-guidebook-knockdown =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } нокдаун минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } с накоплением
       *[set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } нокдаун минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } без накопления
        [remove]
            { $chance ->
                [1] Убирает
               *[other] убирают
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } нокдауна
    }
entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Устанавливает
       *[other] устанавливают
    } температуру раствора ровно на { NATURALFIXED($temperature, 2) }K
entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Убирает
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] убирают
            }
    } тепло из раствора, пока оно не достигнет { $deltasign ->
        [1] максимум { NATURALFIXED($maxtemp, 2) }K
       *[-1] минимум { NATURALFIXED($mintemp, 2) }K
    }
entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) }u реагента { $reagent } { $deltasign ->
        [1] в раствор
       *[-1] из раствора
    }
entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) }u реагентов группы { $group } { $deltasign ->
        [1] в раствор
       *[-1] из раствора
    }
entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Забирает
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] забирают
            }
    } { POWERJOULES($amount) } тепла { $deltasign ->
        [1] телу, в котором находится
       *[-1] у тела, в котором находится
    }
entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезнь { $disease }
entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезни: { $diseases }
entity-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } дрожь
entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Очищает
       *[other] очищают
    } кровоток от других химических веществ
entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Излечивает
       *[other] излечивают
    } болезни
entity-effect-guidebook-eye-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Наносит
               *[-1] Лечит
            }
       *[other]
            { $deltasign ->
                [1] наносят
               *[-1] лечат
            }
    } повреждения глаз
entity-effect-guidebook-vomit =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } рвоту
entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $moles } { $moles ->
        [one] моль
        [few] моля
       *[other] молей
    } газа { $gas }
entity-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } опьянение
entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Бьёт током
       *[other] бьют током
    } цель метаболизма на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
entity-effect-guidebook-emote =
    { $chance ->
        [1] Заставит
       *[other] заставят
    } цель метаболизма издать эмоцию [bold][color=white]{ $emote }[/color][/bold]
entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Тушит
       *[other] тушат
    } огонь
entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } горючесть
entity-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
       *[other] поджигают
    } цель метаболизма
entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Дарует
       *[other] даруют
    } цели метаболизма разум
entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Полиморфирует
       *[other] полиморфируют
    } цель метаболизма в { $entityname }
entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Вызывает
               *[-1] Уменьшает
            }
       *[other]
            { $deltasign ->
                [1] вызывают
               *[-1] уменьшают
            }
    } кровотечение
entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Увеличивает
               *[-1] Снижает
            }
       *[other]
            { $deltasign ->
                [1] увеличивают
               *[-1] снижают
            }
    } уровень крови
entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
       *[other] парализуют
    } цель метаболизма минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } скорость передвижения в { NATURALFIXED($sprintspeed, 3) } раза минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Временно предотвращает
       *[other] временно предотвращают
    } приступы нарколепсии
entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
       *[other] смывают
    } кремовый пирог с лица
entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Излечивает
       *[other] излечивают
    } текущую зомби-инфекцию
entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Заражает
       *[other] заражают
    } существо зомби-инфекцией
entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Излечивает
       *[other] излечивают
    } зомби-инфекцию и дает иммунитет к будущим заражениям
entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Регенерирует
       *[other] регенерируют
    } { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    } гниения
entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } реакцию дыма или пены на протяжении { NATURALFIXED($duration, 3) } { $duration ->
        [one] секунды
        [few] секунды
       *[other] секунд
    }
entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Добавляет
       *[other] добавляют
    } { $reagent } во внутренний контейнер для раствора
entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Помогает
       *[other] помогают
    } разблокировать инопланетный артефакт.
entity-effect-guidebook-artifact-durability-restore = Восстанавливает { $restored } прочности в активных узлах инопланетного артефакта.
entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } { $attribute } на { $positive ->
        [true] [color=red]{ $amount }[/color]
       *[false] [color=green]{ $amount }[/color]
    }
entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
       *[other] омолаживают
    } растение в зависимости от его возраста и времени роста
entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } жизнеспособность растения, утраченную из-за мутации
entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Увеличивает
       *[other] увеличивают
    } продолжительность жизни и/или базовое здоровье растения с шансом 10% для каждого параметра
entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Увеличивает
       *[other] увеличивают
    } потенцию растения на { $increase } до максимума { $limit }. Заставляет растение терять семена, когда потенция достигает { $seedlesstreshold }. Попытка увеличить потенцию выше { $limit } может снизить урожайность с шансом 10%
entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } семена растения
entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Удаляет
       *[other] удаляют
    } семена растения
entity-effect-guidebook-plant-mutate-chemicals =
    { $chance ->
        [1] Мутирует
       *[other] мутируют
    } растение для производства { $name }
