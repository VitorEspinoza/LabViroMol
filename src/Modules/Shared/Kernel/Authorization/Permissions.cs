namespace LabViroMol.Modules.Shared.Kernel.Authorization;

public static class Permissions
{
    public const string View = "View";
    public const string Manage = "Manage";

    public static class Identity
    { 
        public const string UsersManage = $"Identity.Users.{Manage}";
        public const string UsersView = $"Identity.Users.{View}";

        public const string RolesView = $"Identity.Roles.{View}";
        public const string RolesManage = $"Identity.Roles.{Manage}";
    }

    public static class Research
    {
        public const string ProjectsView = $"Research.Projects.{View}";
        public const string ProjectsManage = $"Research.Projects.{Manage}";
        public const string PublicationsView = $"Research.Publications.{View}";
        public const string PublicationsManage = $"Research.Publications.{Manage}";
        public const string ResearchersView = $"Research.Researchers.{View}";
        public const string ResearchersManage = $"Research.Researchers.{Manage}";
        public const string PartnersView = $"Research.Partners.{View}";
        public const string PartnersManage = $"Research.Partners.{Manage}";
        public const string PositionsView = $"Research.Positions.{View}";
        public const string PositionsManage = $"Research.Positions.{Manage}";
    }

    public static class Inventory
    {
        public const string StockView = $"Inventory.Stock.{View}";
        public const string StockManage = $"Inventory.Stock.{Manage}";
        public const string MaterialsView = $"Inventory.Materials.{View}";
        public const string MaterialsManage = $"Inventory.Materials.{Manage}";
        public const string KitsView = $"Inventory.Kits.{View}";
        public const string KitsManage = $"Inventory.Kits.{Manage}";
        public const string OrdersView = $"Inventory.Orders.{View}";
        public const string OrdersManage = $"Inventory.Orders.{Manage}";
    }

    public static class Assets
    {
        public const string EquipmentsView = $"Assets.Equipments.{View}";
        public const string EquipmentsManage = $"Assets.Equipments.{Manage}";
        public const string MaintenanceView = $"Assets.Maintenance.{View}";
        public const string MaintenanceManage = $"Assets.Maintenance.{Manage}";
    }

    public static class Scheduling
    {
        public const string SchedulesView = $"Scheduling.Schedules.{View}";
        public const string SchedulesManage = $"Scheduling.Schedules.{Manage}";
    }
}
