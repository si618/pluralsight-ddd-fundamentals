using Ardalis.Specification;

namespace FrontDesk.Core.SyncedAggregates.Specifications
{
    public class ClientsIncludePatientsSpecification : Specification<Client>
    {
        public ClientsIncludePatientsSpecification()
        {
            Query
              .Include(client => client.Patients)
              .OrderBy(client => client.FullName);
        }
    }
}
