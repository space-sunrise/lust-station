import os
import sys
import json
import subprocess
from azure.ai.inference import ChatCompletionsClient
from azure.ai.inference.models import SystemMessage, UserMessage
from azure.core.credentials import AzureKeyCredential

def get_diff():
    """Reads DIFF from file passed by GitHub Actions."""
    with open("diff.txt", "r", encoding="utf-8") as f:
        return f.read()

def get_gdd():
    """Loads GDD file from repo."""
    with open(".github/gdd.md", "r", encoding="utf-8") as f:
        return f.read()

def get_pr_title():
    with open("pr_title.txt", "r", encoding="utf-8") as f:
        return f.read().strip()

def get_pr_body():
    with open("pr_body.txt", "r", encoding="utf-8") as f:
        return f.read().strip()

def run_ai_review(gdd, diff):
    endpoint = "https://models.github.ai/inference"
    model_name = "microsoft/Phi-4-reasoning"
    token = os.environ["GITHUB_TOKEN"]
    
    client = ChatCompletionsClient(
        endpoint=endpoint,
        credential=AzureKeyCredential(token),
    )

    title = get_pr_title()
    body = get_pr_body()
    
    prompt = f"""
    Ты — главный геймдизайнер проекта Sunrise Station.
    
    Проанализируй Pull Request.
    
    === TITLE ===
    {title}
    
    === DESCRIPTION ===
    {body}
    
    === GAME DESIGN DOCUMENT ===
    {gdd}
    
    === PULL REQUEST DIFF ===
    {diff}
    
    Ответь:
    1. Соответствует ли PR идеям GDD?
    2. Понижает или повышает ли баланс?
    3. Влияет ли на прогрессию отделов?
    4. Есть ли нарушения RP-принципов?
    5. Дай рекомендации.
    """

    
    response = client.complete(
        messages=[
            SystemMessage(content="You are an expert senior game designer."),
            UserMessage(content=prompt),
        ],
        temperature=0.3,
        top_p=1.0,
        max_tokens=5000,
        model=model_name
    )

    return response.choices[0].message.content

def main():
    diff = get_diff()
    gdd = get_gdd()
    review = run_ai_review(gdd, diff)

    with open("ai_review.txt", "w", encoding="utf-8") as f:
        f.write(review)

    print("AI review complete.")


if __name__ == "__main__":
    main()
