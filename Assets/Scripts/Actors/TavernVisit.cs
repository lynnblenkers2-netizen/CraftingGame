using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Actors/Tasks/Tavern Visit", fileName = "TavernVisit_")]
public class TavernVisit : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("Cost")]
    [Min(0)] public int goldCost = 5;

    [Header("Duration (game days)")]
    [Min(0.1f)] public float visitDays = 0.3f;

    [Header("Chances")]
    [Range(0f, 1f)] public float chanceForageLead = 0.25f;
    [Range(0f, 1f)] public float chanceSellLead = 0.25f;
    [Range(0f, 1f)] public float chanceResearchLead = 0.2f;
    [Range(0f, 1f)] public float chanceNewActorOffer = 0.05f;

    [Header("Discoverable Leads")]
    public List<ForageArea> forageLeads = new();
    public List<SellRoute> sellLeads = new();
    public List<ResearchDomain> researchLeads = new();
    public List<ActorDefinition> actorOffers = new();

    [Header("Strings")]
    [SerializeField] public string msgHeadingToTavern = "Visiting tavern.";
    [SerializeField] public string msgNotEnoughGold = "Not enough gold for tavern ({0}g needed).";
    [SerializeField] public string msgNoDiscovery = "No useful rumors today.";
    [SerializeField] public string msgFoundForage = "Found new forage area: {0}.";
    [SerializeField] public string msgFoundSell = "Found new sell route: {0}.";
    [SerializeField] public string msgFoundResearch = "Met scholar about {0}.";
    [SerializeField] public string msgActorOfferTitle = "Tavern Hire";
    [SerializeField] public string msgActorOfferBody = "A drifter offers to join: {0}. Hire them for free?";
    [SerializeField] public string msgActorOfferAccept = "Hire";
    [SerializeField] public string msgActorOfferDecline = "Decline";
    [SerializeField] public string msgActorHireCost = "Hire cost: {0} gold (you have {1}).";
    [SerializeField] public string msgActorHireCostFree = "Hire cost: free.";
    [SerializeField] public string msgActorHired = "{0} joined the crew.";
    [SerializeField] public string msgActorDeclined = "Turned down a potential hire.";
    [SerializeField] public string msgRosterFull = "Roster is full.";
}
