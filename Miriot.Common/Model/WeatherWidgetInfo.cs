using Miriot.Common.Model.WidgetInfoModel;

namespace Miriot.Common.Model
{
    public class WeatherWidgetInfo : WidgetInfo
    {
        public string Location { get; set; }
        public Weather Weather { get; set; }
        public int Temperature { get; set; }
        public Wind Wind { get; set; }
    }
}
