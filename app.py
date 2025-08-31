from fastapi import FastAPI, HTTPException
from FunPayAPI.account import Account
from pydantic import BaseModel
from FunPayAPI.common import enums
import asyncio
from concurrent.futures import ThreadPoolExecutor

app = FastAPI()

class AuthRequest(BaseModel):
    golden_key: str
    user_agent: str = "Mozilla/5.0"

@app.post("/auth")
async def authenticate(request: AuthRequest):
    try:
        account = Account(request.golden_key, user_agent=request.user_agent)
        account.get()
        return {"username": account.username, "id": account.id}
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@app.get("/lots/{subcategory_id}")
async def get_lots(subcategory_id: int, golden_key: str):
    try:
        loop = asyncio.get_event_loop()
        with ThreadPoolExecutor() as executor:
            account = Account(golden_key)
            await loop.run_in_executor(executor, account.get)
            
            lots = await loop.run_in_executor(
                executor, 
                account.get_subcategory_public_lots, 
                enums.SubCategoryTypes.COMMON,  # ✅ Теперь так
                subcategory_id
            )
            
        return [{"id": lot.id, "price": lot.price, "description": lot.description} for lot in lots]
    except Exception as e:
        print(f"Error: {e}")
        raise HTTPException(status_code=400, detail=str(e))