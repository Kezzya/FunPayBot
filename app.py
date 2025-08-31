from fastapi import FastAPI, HTTPException
from FunPayAPI.account import Account
from pydantic import BaseModel
from FunPayAPI.common import enums
from FunPayAPI.types import LotFields
import asyncio
from concurrent.futures import ThreadPoolExecutor
import logging

# Настройка логирования
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI()

class AuthRequest(BaseModel):
    golden_key: str
    user_agent: str = "Mozilla/5.0"

class CreateLotRequest(BaseModel):
    subcategory_id: int
    price: float
    description: str

@app.post("/auth")
async def authenticate(request: AuthRequest):
    try:
        account = Account(request.golden_key, user_agent=request.user_agent)
        account.get()
        return {"username": account.username, "id": account.id}
    except Exception as e:
        logger.error(f"Auth error: {str(e)}")
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
                enums.SubCategoryTypes.COMMON,
                subcategory_id
            )
            
        logger.info(f"Retrieved {len(lots)} lots for subcategory {subcategory_id}")
        return [
            {
                "id": lot.id,
                "price": lot.price,
                "description": lot.description,
                "seller_id": lot.seller.id,
                "seller_username": lot.seller.username
            } for lot in lots
        ]
    except Exception as e:
        logger.error(f"Error getting lots for subcategory {subcategory_id}: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))

@app.get("/lots-by-user/{subcategory_id}/{user_id}")
async def get_lots_by_user(subcategory_id: int, user_id: int, golden_key: str):
    try:
        loop = asyncio.get_event_loop()
        with ThreadPoolExecutor() as executor:
            account = Account(golden_key)
            await loop.run_in_executor(executor, account.get)
            
            lots = await loop.run_in_executor(
                executor, 
                account.get_subcategory_public_lots, 
                enums.SubCategoryTypes.COMMON,
                subcategory_id
            )
            
        user_lots = [
            {
                "id": lot.id,
                "price": lot.price,
                "description": lot.description,
                "seller_id": lot.seller.id,
                "seller_username": lot.seller.username
            } for lot in lots if lot.seller.id == user_id
        ]
        
        if not user_lots:
            logger.warning(f"No lots found for user {user_id} in subcategory {subcategory_id}")
            raise HTTPException(status_code=404, detail="No lots found for this user in the specified subcategory")
            
        logger.info(f"Retrieved {len(user_lots)} lots for user {user_id} in subcategory {subcategory_id}")
        return user_lots
    except Exception as e:
        logger.error(f"Error getting lots for user {user_id} in subcategory {subcategory_id}: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))

@app.post("/create-lot")
async def create_lot(request: CreateLotRequest, golden_key: str):
    try:
        loop = asyncio.get_event_loop()
        with ThreadPoolExecutor() as executor:
            account = Account(golden_key)
            await loop.run_in_executor(executor, account.get)
            
            lot_fields = LotFields(
                lot_id=0,  # 0 для нового лота
                fields={
                    "csrf_token": account.csrf_token,
                    "subcategory_id": request.subcategory_id,
                    "price": str(request.price),
                    "description": request.description
                }
            )
            
            await loop.run_in_executor(executor, account.save_lot, lot_fields)
            
        logger.info(f"Created lot in subcategory {request.subcategory_id}")
        return {
            "id": 0,  # FunPayAPI не возвращает ID нового лота, используем 0 как заглушку
            "price": request.price,
            "description": request.description,
            "seller_id": account.id,
            "seller_username": account.username
        }
    except Exception as e:
        logger.error(f"Error creating lot in subcategory {request.subcategory_id}: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))