namespace CESI_CI_CD.ApiService.Data.Entities;

public enum ListingStatus
{
    Published,
    Rejected,
    Pending,
    // Ajouté en fin d'enum : les valeurs existantes (0/1/2) restent stables, migration
    // non destructive pour les lignes déjà en base.
    Sold,
}
