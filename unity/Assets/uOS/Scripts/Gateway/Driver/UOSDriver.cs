using System.Collections.Generic;


namespace UOS
{
    public interface UOSDriver
    {
        UpDriver GetDriver();

        IList<UpDriver> GetParent();

        void Init(IGateway gateway, uOSSettings settings, string instanceId);

        void Destroy();
    }
}
