Scene2: спавн и выход из участка

Игрок InvestigationPlayer начинает у imagecheck/CubeStart.
CubeStart сохранён как маркер, его MeshRenderer и BoxCollider выключены.
imagecheck/PlayerSpawn совпадает с начальной точкой игрока.

imagecheck/StationExit_1 и StationExit_2 — зоны подхода с BoxCollider (Is Trigger).
StationExit.apartmentSpawn назначен на americannredotoday1/PlayerSpawn.
PlayerInteraction.nearbyExits содержит обе зоны. Подсказка: «E - покинуть участок».
Перемещение происходит внутри Scene2; сцены Day1–Day3 не загружаются.
PlayerController.Teleport сбрасывает вертикальную скорость и наклон камеры.
Новые Rigidbody не добавлены.

Проверка: открыть Assets/Scenes/Scene2.unity, Play, подойти к любой двери и нажать E.
Можно смотреть в любую сторону. При открытом осмотре/блокировке управления выход недоступен.
Все компоненты и ссылки уже назначены; добавлять скрипты вручную не требуется.

Проверено в Unity 6000.6.8f1 Play Mode:
- начальная точка и точка прибытия устойчиво стоят на полу;
- обе зоны распознаются;
- реальные события клавиши E через Input System выполняют оба переноса;
- подсказка очищается после переноса;
- внешний запрет управления блокирует выход.
Сцена сохранена, Play Mode остановлен, ошибок компиляции нет.
