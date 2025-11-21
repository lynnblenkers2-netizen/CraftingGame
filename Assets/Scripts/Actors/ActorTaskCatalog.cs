using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Task Catalog", fileName = "ActorTaskCatalog")]
public class ActorTaskCatalog : ScriptableObject
{
    public List<ForageArea> forageAreas = new();
    public List<SellRoute> sellRoutes = new();
    public List<ResearchDomain> researchDomains = new();
    public List<CraftPlan> craftPlans = new();
    public List<TavernVisit> tavernVisits = new();
}
