"use strict";

function convertToEvent(full) {
    return {
        id: full[0],
        utilizer_type: full[1],
        utilizer_id: full[2],
        utilizer_name: full[3],
        event_type: full[4],
        event_time: full[5]
    };
}

function getEventIconClass(eventType) {
    if (eventType === "TokenGenerated")
    {
        return "key";
    }
    else if (eventType === "TokenRefreshed")
    {
        return "key";
    }
    else if (eventType === "TokenRevoked")
    {
        return "key";
    }
    else if (eventType === "TokenVerified")
    {
        return "key";
    }
    else if (eventType.includes("User"))
    {
        return "person-fill";
    }
    else if (eventType.includes("Role"))
    {
        return "shield-shaded";
    }
    else if (eventType.includes("Application"))
    {
        return "laptop";
    }
    else if (eventType.includes("Membership"))
    {
        return "house";
    }
    else if (eventType.includes("Provider"))
    {
        return "boxes";
    }
    else if (eventType.includes("Webhook"))
    {
        return "broadcast";
    }
    else
    {
        return "question-octagon";
    }
}

function getEventColorClass(eventType) {
    if (eventType === "TokenGenerated")
    {
        return "primary";
    }
    else if (eventType === "TokenRefreshed")
    {
        return "warning";
    }
    else if (eventType === "TokenRevoked")
    {
        return "danger";
    }
    else if (eventType === "TokenVerified")
    {
        return "info";
    }
    else
    {
        if (eventType.includes("Create"))
        {
            return "success";
        }
        else if (eventType.includes("Update"))
        {
            return "warning";
        }
        else if (eventType.includes("Delete"))
        {
            return "danger";
        }
        else
        {
            return "dark";
        }
    }
}