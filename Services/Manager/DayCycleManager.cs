using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public static class DayCycleManager
    {
        private static DateOnly _lastDay; 
        private static bool _running = false;
        private static GameStatus _gameStatus = new();
        public static TimeSegment CurrentTimeSegment { get; private set; }
        /// <summary>
        /// initialises the session time
        /// </summary>
        public static void Initialize()
        {
            var now = DateTime.Now;

            _lastDay = DateOnly.FromDateTime(GameService.Game.LastUpdateTime);
            UpdateTimeSegment(now.Hour);

            // Roll gathering limits for the new day
            foreach (var room in RoomService.AllRooms)
            {
                if (room.GatheringSpots.Any())
                    room.RollGatherLimit();
            }
            StartBackgroundLoop();
        }
        /// <summary>
        /// starts the background time sync
        /// </summary>
        /// <param name="intervalMs"></param>
        public static void StartBackgroundLoop(int intervalMs = 30000)
        {
            if (_running) return;
            _running = true;

            Task.Run(async () =>
            {
                while (_running)
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception ex)
                    {
                    }

                    await Task.Delay(intervalMs);
                }

            });

        }
        /// <summary>
        /// Updates the Session time
        /// </summary>
        public static void Update()
        {
            var now = DateTime.Now;

            if (DateOnly.FromDateTime(now) != _lastDay)
            {
                _lastDay = DateOnly.FromDateTime(now);

                foreach (var room in RoomService.AllRooms)
                {
                    if (room.GatheringSpots.Any())
                        room.RollGatherLimit(); 
                    if (UserAccoundService.CurrentCharacter != null)
                        UserAccoundService.CurrentCharacter.RoomGatheringStatus.Clear();
                }

            }

            _gameStatus.LastUpdateTime = DateTime.Now;
            GameStatusService.Save(_gameStatus);

            UpdateTimeSegment(now.Hour);
        }
        /// <summary>
        /// Updates the day time segement
        /// </summary>
        /// <param name="hour">current hour</param>
        private static void UpdateTimeSegment(int hour)
        {
            TimeSegment previousTime = CurrentTimeSegment;
            CurrentTimeSegment = hour switch
            {
                >= 6 and < 12 => TimeSegment.Morning,
                >= 12 and < 17 => TimeSegment.Midday,
                >= 17 and < 21 => TimeSegment.Evening,
                _ => TimeSegment.Night
            };

        }

    }

}
