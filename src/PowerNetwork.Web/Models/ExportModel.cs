namespace PowerNetwork.Web.Models
{
    public class ExportModel
    {
        public decimal zoom { get; set; }
        public ExportCenterModel center { get; set; }
        public ExportBoundsModel bounds { get; set; }
        public ExportFiltersModel filters { get; set; }
    }

    public class ExportCenterModel
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }

    public class ExportBoundsModel
    {
        public decimal east { get; set; }
        public decimal west { get; set; }
        public decimal north { get; set; }
        public decimal south { get; set; }
    }

    public class ExportFiltersModel
    {
        public string region { get; set; }
        public string city { get; set; }
        public string center { get; set; }
        public string ctCode { get; set; }
        public string meterCode { get; set; }
    }
}