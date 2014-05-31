namespace UOS
{
    /// <summary>
    /// Interface of all uOS Radars.
    /// </summary>
    public interface IRadar
    {
        /// <summary>
        /// Starts the radar.
        /// </summary>
        void Init();

        /// <summary>
        /// Stops the radar.
        /// </summary>
        void TearDown();
    }
}
