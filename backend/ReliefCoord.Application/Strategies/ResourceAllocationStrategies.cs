using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Application.Strategies;

public class MultiConstraintOptimalStrategy : IResourceAllocationStrategy
{
    public string StrategyName => "MultiConstraintOptimal";

    public ResourceAllocationPlanResult ComputeAllocationPlan(
        IReadOnlyList<AssistanceRequest> competingRequests,
        IReadOnlyList<ResourceItem> candidateResources,
        IMappingProviderService mappingService)
    {
        var allocations = new List<PlannedAllocationItemDto>();
        var remainingStockByWarehouse = new Dictionary<Guid, int>();

        foreach (var resource in candidateResources)
        {
            remainingStockByWarehouse[resource.Id] = resource.AvailableQuantity;
        }

        var sortedRequests = competingRequests
            .OrderByDescending(r => (int)r.FinalApprovedUrgency)
            .ThenByDescending(r => r.PriorityScore)
            .ToList();

        int totalRequested = 0;
        int totalAllocated = 0;
        bool shortageEncountered = false;

        foreach (var request in sortedRequests)
        {
            totalRequested += request.RequestedQuantity;

            ResourceItem? bestResource = null;
            double bestScore = -1.0;
            double bestDistanceKm = 0.0;

            foreach (var resource in candidateResources)
            {
                int currentAvailable = remainingStockByWarehouse[resource.Id];
                if (resource.Category != request.Category || currentAvailable <= 0)
                {
                    continue;
                }

                double distanceKm = mappingService.CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    resource.WarehouseLatitude,
                    resource.WarehouseLongitude);

                double proximityScore = Math.Max(0.0, 100.0 - (distanceKm * 1.4));

                double stockAdequacyScore = 100.0;
                if (currentAvailable < request.RequestedQuantity)
                {
                    stockAdequacyScore = (currentAvailable / (double)request.RequestedQuantity) * 100.0;
                }

                double expiryScore = 65.0;
                if (resource.ExpiryDateUtc.HasValue)
                {
                    double daysLeft = (resource.ExpiryDateUtc.Value - DateTime.UtcNow).TotalDays;
                    if (daysLeft <= 30)
                    {
                        expiryScore = 95.0;
                    }
                    else if (daysLeft <= 90)
                    {
                        expiryScore = 80.0;
                    }
                }

                double coldChainScore = (request.Category == RequestCategory.Medicine && resource.SupportsColdChain) ? 100.0 : 75.0;
                double orgAlignmentScore = (request.AssignedOrganisationId == resource.OwningOrganisationId) ? 100.0 : 70.0;

                double totalWeightedScore =
                    (proximityScore * 0.35) +
                    (stockAdequacyScore * 0.25) +
                    (expiryScore * 0.15) +
                    (coldChainScore * 0.15) +
                    (orgAlignmentScore * 0.10);

                totalWeightedScore = Math.Round(totalWeightedScore, 2);

                if (totalWeightedScore > bestScore)
                {
                    bestScore = totalWeightedScore;
                    bestResource = resource;
                    bestDistanceKm = Math.Round(distanceKm, 2);
                }
            }

            if (bestResource == null)
            {
                shortageEncountered = true;
                allocations.Add(new PlannedAllocationItemDto(
                    request.Id,
                    request.ReferenceCode,
                    request.Title,
                    request.FinalApprovedUrgency,
                    request.PriorityScore,
                    Guid.Empty,
                    "No Matching Inventory",
                    "Partner Escalation Queue",
                    request.RequestedQuantity,
                    0,
                    true,
                    0,
                    0,
                    "Stock exhausted for category. Triggered Partner Aid Request."
                ));
                continue;
            }

            int availableAtBest = remainingStockByWarehouse[bestResource.Id];
            int allocatedQty = Math.Min(request.RequestedQuantity, availableAtBest);
            remainingStockByWarehouse[bestResource.Id] -= allocatedQty;
            totalAllocated += allocatedQty;

            bool isPartialShortage = allocatedQty < request.RequestedQuantity;
            if (isPartialShortage)
            {
                shortageEncountered = true;
            }

            string recommendationNote = "Primary multi-constraint selection satisfies full demand.";
            if (isPartialShortage)
            {
                int deficit = request.RequestedQuantity - allocatedQty;
                recommendationNote = $"Shortfall of {deficit} units. Recommend secondary warehouse dispatch or Partner Organisation transfer.";
            }

            allocations.Add(new PlannedAllocationItemDto(
                request.Id,
                request.ReferenceCode,
                request.Title,
                request.FinalApprovedUrgency,
                request.PriorityScore,
                bestResource.Id,
                bestResource.Name,
                bestResource.WarehouseName,
                request.RequestedQuantity,
                allocatedQty,
                isPartialShortage,
                bestScore,
                bestDistanceKm,
                recommendationNote
            ));
        }

        int unmetShortfall = Math.Max(0, totalRequested - totalAllocated);

        return new ResourceAllocationPlanResult(
            StrategyName,
            shortageEncountered,
    }
}
