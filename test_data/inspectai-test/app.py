from inspect_ai import Task, task
from inspect_ai.dataset import Sample
from inspect_ai.scorer import (
    exact, choice)
from inspect_ai.solver import (
    multiple_choice, generate, system_message)
import json

sampleFile = open('../sample_data.json', 'r')
content = sampleFile.read()
data = json.loads(content)

def mapTarget(distinctChoices, target):
    idx = distinctChoices.index(target)
    match idx:
        case 0:
            return 'A'
        case 1:
            return 'B'
        case 2:
            return 'C'
        case 3:
            return 'D'

distinctOutputs = list(set([item["output"] for item in data]))
disinctStr = " or ".join(distinctOutputs)

multipleChoiceTests = [Sample(
    input= "Anaylze this review: \n" + item["input"],
    target=mapTarget(distinctOutputs, item["output"]),
    choices = distinctOutputs
) for item in data]

answerRelevancyTests = [Sample(
    input= [
        {
            "role": "system",
            "content": "You're a product review expert. If the product review is positive, return Positive.\n"
            + "If the product review is negative, return Negative.\n" 
            + "If the product review is neither positive or negative, return Neutral.\n" 
            + "Do not return any other text."
        },
        {
            "role": "user",
            "content": "Anaylze this review: \n" + item["input"]
        }
    ],
    target= item["output"],
) for item in data]

@task
def MultipleChoice():
    return Task(
        dataset=multipleChoiceTests,
        solver=[
            multiple_choice() # this will call Generate() under the hood
        ],
        scorer=choice()
    )

@task
def AnswerRelevancy():
    return Task(
        dataset=answerRelevancyTests,
        solver=[
            generate() # this will call Generate() under the hood
        ],
        scorer=exact()
    )

