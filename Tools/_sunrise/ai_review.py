import os
import sys
import json
import subprocess
from mistralai import Mistral, UserMessage, SystemMessage

def get_diff():
    """Reads DIFF from file passed by GitHub Actions."""
    with open("diff.txt", "r", encoding="utf-8") as f:
        return f.read()

def get_gdd():
    """Loads GDD file from repo."""
    with open("docs/gdd.md", "r", encoding="utf-8") as f:
        return f.read()

def run_ai_review(gdd, diff):
    client = Mistral(
        api_key=os.environ["GITHUB_TOKEN"],
        server_url="https://models.github.ai/inference"
    )

    prompt = f"""
Ты — главный геймдизайнер проекта.

Проанализируй изменения, которые внесены в Pull Request, и оцени:

1. Соответствуют ли изменения GDD?
2. Есть ли риски для баланса?
3. Нарушают ли они экономику / прогрессию / архитектуру игровых систем?
4. Дай рекомендации по исправлению, если есть несоответствия.

=== GAME DESIGN DOCUMENT ===
{gdd}

=== PULL REQUEST DIFF ===
{diff}
"""

    response = client.chat(
        model="mistral-ai/Codestral-2501",
        messages=[
            SystemMessage("You are an expert senior game designer."),
            UserMessage(prompt),
        ],
        temperature=0.3,
        max_tokens=1500,
        top_p=1.0
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
