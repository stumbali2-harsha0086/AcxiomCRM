namespace AcxiomCRM.Domain.Entities
{
    public enum LeadStatus
    {
        New,
        Contacted,
        Qualified,
        Unqualified,
        Converted,
        Lost
    }

    public enum OpportunityStage
    {
        Qualification,
        Proposal,
        Negotiation,
        Won,
        Lost
    }

    public enum OpportunityStatus
    {
        Open,
        Won,
        Lost,
        Cancelled
    }

    public enum FollowUpType
    {
        Call,
        Meeting,
        Email,
        Task
    }

    public enum FollowUpStatus
    {
        Planned,
        Completed,
        Missed,
        Cancelled
    }

    public enum ActivityType
    {
        Call,
        Meeting,
        Email,
        Task
    }

    public enum ActivityStatus
    {
        Planned,
        Completed,
        InProgress,
        Cancelled
    }
}
