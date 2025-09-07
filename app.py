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
        
        
        @app.post("/copy_lots")
async def copy_lots_by_user_id(user_id: int, subcategory_id: Optional[int] = None, account: Account = Depends(get_account)) -> List[dict]:
    logger.info(f"Copying lots for user ID: {user_id}, subcategory: {subcategory_id}")

    if subcategory_id is None or subcategory_id == -1:
        subcategories_to_process = get_user_subcategories(user_id, account)
        logger.info(f"Found {len(subcategories_to_process)} subcategories for user {user_id}")
    else:
        subcategories_to_process = [subcategory_id]

    all_created_lots = []

    for subcat in subcategories_to_process:
        try:
            lots_from_subcategory = copy_lots_from_subcategory(user_id, subcat, account)
            all_created_lots.extend(lots_from_subcategory)
        except Exception as ex:
            logger.error(f"Error copying lots from subcategory {subcat} for user {user_id}", exc_info=ex)

    logger.info(f"Successfully copied {len(all_created_lots)} lots total for user ID: {user_id}")
    return all_created_lots

def get_user_subcategories(user_id: int, account: Account) -> List[int]:
    user = account.get_user(user_id)
    return list(set(lot.subcategory.id for lot in user.lots if lot.subcategory.type == SubCategoryTypes.COMMON))

def copy_lots_from_subcategory(user_id: int, subcat: int, account: Account) -> List[dict]:
    lots = account.get_subcategory_public_lots(SubCategoryTypes.COMMON, subcat)
    user_lots: List[LotShortcut] = [lot for lot in lots if lot.seller.user_id == user_id]
    created = []

    for lot in user_lots:
        lot_page: LotPage = account.get_lot_page(lot.id)
        # Get empty fields for new lot in subcategory
        response = account.method("get", f"lots/offerEdit?offer=0&node={subcat}", {}, {}, raise_not_200=True)
        bs = BeautifulSoup(response.content.decode(), "lxml")
        fields = {}
        fields.update({field["name"]: field.get("value") or "" for field in bs.find_all("input") if "name" in field.attrs})
        fields.update({field["name"]: field.text or "" for field in bs.find_all("textarea") if "name" in field.attrs})
        fields.update({
            field["name"]: field.find("option", selected=True)["value"]
            for field in bs.find_all("select") if "name" in field.attrs and "hidden" not in field.find_parent(class_="form-group").get("class", [])
        })
        fields.update({field["name"]: "on" for field in bs.find_all("input", {"type": "checkbox"}, checked=True) if "name" in field.attrs})

        # Fill fields from copied lot
        fields["csrf_token"] = account.csrf_token
        fields["offer_id"] = "0"
        fields["node_id"] = str(subcat)
        fields["price"] = str(lot.price)
        if lot.description:
            fields["short_description"] = lot.description
        if lot_page.detailed_description:
            fields["description"] = lot_page.detailed_description
        if lot.server:
            fields["param_0"] = lot.server  # Assume param_0 is server, adjust based on actual fields
        if lot.amount:
            fields["quantity"] = str(lot.amount)  # Assume field name
        if lot.auto:
            fields["auto_delivery"] = "on"  # Assume
        # For images
        photo_ids = []
        for img_url in lot_page.image_urls:
            img_data = requests.get(img_url).content
            photo_id = account.upload_image(img_data, "offer")
            photo_ids.append(photo_id)
        for idx, pid in enumerate(photo_ids):
            fields[f"photos[{idx}]"] = str(pid)  # Assume multi photos field format

        # Save new lot
        new_lot = LotFields(0, fields, lot.subcategory, lot.currency, None)  # Calc result None or compute
        account.save_lot(new_lot)
        created.append({"id": "new_lot_id_placeholder", "subcategory": subcat})  # Adjust to return actual response

    return created