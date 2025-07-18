﻿using System.Text.Json;
using EvalSharp.Models;

namespace EvalSharp.Scoring.TaskCompletion;
internal static class TaskCompletionTemplate
{
    private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    //Prompt adapted from deepeval, licensed under Apache 2.0
    //DeepEval Repo: https://github.com/confident-ai/deepeval
    public static string GenerateGoalAndOutcome(string input, string actualOutput, List<ToolCall> toolsCalled)
    {
        string toolsJson = PrintToolsCalled(toolsCalled);
        return $$"""
        Given an agentic workflow comprised of a human input, AI response, and tools used by the AI, identify the user_goal (the task or objective the user wants to achieve) and the task_outcome (the final outcome or result of the workflow).
        The task outcome should be solely factual, derived strictly from the workflow (input, response, and tools called), without any reasoning involved.

        Example:
        Example input: Can you help me plan a trip to New York this weekend, including travel, accommodation, and sightseeing?
        Example tools called:
        [
            {
                "name": "flight_search",
                "description": "Search for flights based on destination and date.",
                "reasoning": "The input specifies travel as part of the task. This tool is needed to find flight options based on the user's destination and dates.",
                "output": {
                    "flights": ["Flight A", "Flight B"]
                },
                "input_parameters": {
                    "destination": "New York",
                    "date": "Saturday",
                    "return_date": "Sunday"
                }
            },
            {
                "name": "hotel_search",
                "description": "Search for hotels in the given location.",
                "reasoning": "The input specifies accommodation as part of the task. This tool is needed to find hotel options in the specified location for the provided dates.",
                "output": {
                    "hotels": ["Grand NY Hotel", "Empire Suites"]
                },
                "input_parameters": {
                    "location": "New York",
                    "check_in": "Saturday",
                    "check_out": "Sunday"
                }
            },
            {
                "name": "sightseeing_search",
                "description": "Provide sightseeing options for a given location.",
                "reasoning": "The input specifies sightseeing as part of the task. This tool is needed to generate a list of recommended places to visit in New York.",
                "output": {
                    "sights": ["Central Park", "Statue of Liberty", "Times Square"]
                },
                "input_parameters": {
                    "location": "New York"
                }
            }
        ]
        Example response: Sure! Flights available to New York include Flight A and Flight B. Accommodation options include Grand NY Hotel and Empire Suites. Suggested sightseeing spots in New York are Central Park, Statue of Liberty, and Times Square. 

        Example JSON:
        {
            "user_goal": "Have the system plan a weekend trip to New York, including travel, accommodation, and sightseeing.",
            "task_outcome": "The system provided suggested flights departing on Saturday and returning on Sunday, identified hotels with check-in on Saturday and check-out on Sunday, and generated a list of sightseeing destinations in New York City."
        }
        ===== END OF EXAMPLE ======

        **
        IMPORTANT: Please make sure to only return in JSON format with two keys: `user_goal` and `task_outcome`.
        **

        input: {{input}}
        tools called:
        {{toolsJson}}
        response: {{actualOutput}}

        JSON:
        """;
    }

    //Prompt adapted from deepeval, licensed under Apache 2.0
    //DeepEval Repo: https://github.com/confident-ai/deepeval
    public static string GenerateVerdict(string userGoal, string actualOutcome)
    {
        return $$"""
        Given the user goal (desired outcome) and the actual achieved outcome, compare how well the actual outcome aligns with the user's intended goal.

        Please return a JSON with two keys: `verdict` and `reason`.
        - The `verdict` should be a score from 0 to 1, where 1 indicates the actual outcome perfectly achieves the user's goal, and 0 indicates it does not achieve the goal at all.
        - The `reason` should explain why the given verdict was assigned.

        **
        IMPORTANT: Please make sure to only return in JSON format, with `verdict` as a float between 0 and 1.
        Example:
        User goal: Have the system plan a weekend trip to New York, including travel, accommodation, and sightseeing.
        Actual outcome: The system provided suggested flights departing on Saturday and returning on Sunday, identified hotels with check-in on Saturday and check-out on Sunday, and generated a list of sightseeing destinations in New York City.
        Example JSON:
        {
            "verdict": 0.85,
            "reason": "The system suggested flights, accommodation, and sightseeing options but did not fully plan the trip as expected."
        }
        **

        User goal:
        {{userGoal}}

        Actual outcome:
        {{actualOutcome}}

        JSON:
        """;
    }

    private static string PrintToolsCalled(List<ToolCall> toolsCalledList)
    {
        return JsonSerializer.Serialize(toolsCalledList, _jsonOptions);
    }
}
