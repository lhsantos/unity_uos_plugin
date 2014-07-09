using System.Collections.Generic;


namespace UOS
{
    public interface UOSDriver
    {
        /// <summary>
        /// Returns a description of this driver as an UpDriver object.
        /// </summary>
        /// <returns></returns>
        UpDriver GetDriver();

        /// <summary>
        /// Returns a list of parent drivers, which may be empty or null.
        /// </summary>
        /// <returns></returns>
        List<UpDriver> GetParent();

        /// <summary>
        /// Initialises this driver.
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="settings"></param>
        /// <param name="instanceId"></param>
        void Init(IGateway gateway, uOSSettings settings, string instanceId);

        /// <summary>
        /// Destroys this driver.
        /// </summary>
        void Destroy();
    }
}
