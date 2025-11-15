import os
import sys
import google.generativeai as genai

MODEL = os.environ.get("GEMINI_MODEL", "models/gemini-1.5-pro")
API_KEY = os.environ.get("GOOGLE_API_KEY")
if not API_KEY:
    print("ERROR: set GOOGLE_API_KEY environment variable", file=sys.stderr)
    sys.exit(1)

TEMPERATURE = 0.2
MAX_OUTPUT_TOKENS = int(os.environ.get("MAX_OUTPUT_TOKENS", "1200"))

genai.configure(api_key=API_KEY)

def read_file(path: str) -> str:
    with open(path, "r", encoding="utf-8") as f:
        return f.read()

def call_gemini(system_prompt: str, user_prompt: str, max_output_tokens: int = MAX_OUTPUT_TOKENS) -> str:
    resp = genai.chat.create(
        model=MODEL,
        temperature=TEMPERATURE,
        max_output_tokens=max_output_tokens,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt},
        ],
    )
    # защитный доступ к результату
    # в некоторых версиях ответа текст доступен как response.last["content"][0]["text"]
    try:
        return resp.last["content"][0]["text"]
    except Exception:
        # более универсальная попытка достать безопасный текст
        try:
            return resp.candidates[0].content[0].text
        except Exception:
            return str(resp)

def run_ai_review(gdd: str, diff: str, title: str, body: str) -> str:
    system_prompt = "Ты — главный геймдизайнер проекта Sunrise Station. Проанализируй Pull Request."
    user_prompt = f"""
    
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
    
    return call_gemini(system_prompt, user_prompt)

def main():
    try:
        diff = read_file("diff.txt")
        gdd = read_file(".github/gdd.md")
        title = read_file("pr_title.txt").strip()
        body = read_file("pr_body.txt").strip()
    except FileNotFoundError as e:
        print(f"Missing file: {e}", file=sys.stderr)
        sys.exit(1)

    review = run_ai_review(gdd, diff, title, body)

    with open("ai_review.txt", "w", encoding="utf-8") as f:
        f.write(review)

    print("AI review complete.")

if __name__ == "__main__":
    main()
