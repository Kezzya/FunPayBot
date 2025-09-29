from fastapi import FastAPI, HTTPException, Depends, Query, Form, Request
from FunPayAPI.account import Account
from pydantic import BaseModel
from FunPayAPI.common import enums
from FunPayAPI.common.enums import SubCategoryTypes
from FunPayAPI.types import LotFields, LotShortcut, LotPage
import asyncio
from concurrent.futures import ThreadPoolExecutor
import logging
from typing import List, Optional
from bs4 import BeautifulSoup
import requests
from typing import Annotated

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

class CopyLotsRequest(BaseModel):
    user_id: int
    subcategory_id: Optional[int] = None
    golden_key: str

# Dependency для получения аккаунта
def get_account(golden_key: str) -> Account:
    try:
        account = Account(golden_key)
        account.get()
        return account
    except Exception as e:
        logger.error(f"Failed to create account: {e}")
        raise HTTPException(status_code=400, detail=f"Authentication failed: {str(e)}")

@app.post("/auth")
async def authenticate(request: AuthRequest):
    try:
        account = Account(request.golden_key, user_agent=request.user_agent)
        account.get()
        return {
            "username": account.username,
            "id": account.id,
            "csrftoken": account.csrf_token  # Обязательно вернуть
        }
    except Exception as e:
        logger.error(f"Auth error: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))
@app.get("/lots/offerEdit")
async def get_offer_edit_fields(
    offer: int = Query(0),
    node: int = Query(...),
    golden_key: str = Query(...)
):
    try:
        account = Account(golden_key)
        account.get()
        
        # Получаем HTML страницы редактирования лота
        response = account.method("get", f"lots/offerEdit?offer={offer}&node={node}", {}, {})
        html_content = response.content.decode()
        
        # Парсим поля формы
        fields = parse_lot_fields_from_html(html_content)
        return {"fields": fields, "csrf_token": account.csrf_token}
        
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
            
        user_lots = []
        for lot in lots:
            if lot.seller and lot.seller.id == user_id:
                description_ru = lot.description or ""
                description_en = ""
                title_en = ""
                
                try:
                    lot_page_en = await loop.run_in_executor(
                        executor,
                        account.get_lot_page,
                        lot.id,
                        "en"
                    )
                    title_en = lot_page_en.description or ""
                    description_en = lot_page_en.full_description or ""
                    logger.info(f"Lot {lot.id}: RU='{description_ru[:50]}', EN='{description_en[:50]}', Title EN='{title_en[:50]}'")
                    
                except Exception as e:
                    logger.warning(f"Failed to get English description or title for lot {lot.id}: {e}")
                    description_en = description_ru
                    title_en = lot.title or ""
                
                if not description_en:
                    description_en = description_ru
                if not title_en:
                    title_en = lot.title or ""
                
                lot_data = {
                    "Id": lot.id,
                    "Server": lot.server or "",
                    "Description": lot.description or "",
                    "DescriptionEn": description_en,
                    "Title": lot.title or "",
                    "TitleEn": title_en,
                    "Amount": lot.amount,
                    "Price": lot.price,
                    "Currency": lot.currency.name,
                    "SellerId": lot.seller.id,
                    "SellerUsername": lot.seller.username,
                    "AutoDelivery": lot.auto,
                    "IsPromo": lot.promo,
                    "Attributes": lot.attributes or {},
                    "SubcategoryId": lot.subcategory.id if lot.subcategory else 0,
                    "CategoryName": lot.subcategory.category.name if lot.subcategory and lot.subcategory.category else "",
                    "Html": lot.html,
                    "PublicLink": lot.public_link
                }
                
                user_lots.append(lot_data)
        
        if not user_lots:
            logger.warning(f"No lots found for user {user_id} in subcategory {subcategory_id}")
            raise HTTPException(status_code=404, detail="No lots found for this user in the specified subcategory")
            
        logger.info(f"Retrieved {len(user_lots)} lots for user {user_id} in subcategory {subcategory_id}")
        return user_lots
    except Exception as e:
        logger.error(f"Error getting lots for user {user_id} in subcategory {subcategory_id}: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))
@app.post("/create-lot-from-fields")
async def create_lot_from_fields(
    golden_key: str = Query(...),
    request: Request = None
):
    try:
        # Получаем form-data
        body = await request.body()
        body_str = body.decode('utf-8')
        from urllib.parse import parse_qs, unquote
        parsed = parse_qs(body_str, keep_blank_values=True)
        
        fields = {}
        for key, values in parsed.items():
            fields[key] = unquote(values[0]) if values else ""
        
        logger.info(f"Received fields: {list(fields.keys())}")
        logger.info(f"Fields content: {fields}")
        
        account = Account(golden_key)
        account.get()
        
        # Получаем эталонную форму для сравнения
        subcategory_id = int(fields.get("node_id", 0))
        try:
            reference_response = account.method("get", f"lots/offerEdit?offer=0&node={subcategory_id}", {}, {})
            reference_html = reference_response.content.decode()
            
            # Парсим эталонную форму
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(reference_html, "lxml")
            
            required_fields = set()
            for field in soup.find_all("input"):
                if "name" in field.attrs and field.get("required"):
                    required_fields.add(field["name"])
            
            for field in soup.find_all("select"):
                if "name" in field.attrs and field.get("required"):
                    required_fields.add(field["name"])
                    
            for field in soup.find_all("textarea"):
                if "name" in field.attrs and field.get("required"):
                    required_fields.add(field["name"])
            
            logger.info(f"Required fields found: {required_fields}")
            
            # Проверяем какие поля отсутствуют
            missing_fields = []
            for req_field in required_fields:
                if req_field not in fields or not fields[req_field].strip():
                    missing_fields.append(req_field)
            
            if missing_fields:
                logger.error(f"Missing required fields: {missing_fields}")
            
            # Проверяем все поля формы
            all_form_fields = set()
            for field in soup.find_all(["input", "select", "textarea"]):
                if "name" in field.attrs:
                    all_form_fields.add(field["name"])
            
            logger.info(f"All form fields: {all_form_fields}")
            logger.info(f"Our fields: {set(fields.keys())}")
            logger.info(f"Missing from our request: {all_form_fields - set(fields.keys())}")
            
        except Exception as e:
            logger.warning(f"Could not parse reference form: {e}")
        
        # Обновляем CSRF и отправляем
        fields["csrf_token"] = account.csrf_token
        
        if "price" in fields:
            fields["price"] = fields["price"].replace(",", ".")
        
        headers = {
            "accept": "*/*",
            "content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            "x-requested-with": "XMLHttpRequest",
        }
        
        logger.info(f"Sending to FunPay: {fields}")
        
        response = account.method("post", "lots/offerSave", headers, fields, raise_not_200=False)
        
        logger.info(f"FunPay response status: {response.status_code}")
        logger.info(f"FunPay response content: {response.content.decode()[:500]}")
        
        if response.status_code != 200:
            error_content = response.content.decode()
            raise HTTPException(status_code=400, detail=f"FunPay API error: {response.status_code} - {error_content}")
        
        try:
            json_response = response.json()
            if json_response.get("error"):
                logger.error(f"FunPay returned error: {json_response}")
                raise HTTPException(status_code=400, detail=f"FunPay validation error: {json_response.get('error')}")
            if json_response.get("errors"):
                logger.error(f"FunPay returned errors: {json_response}")
                # Детальные ошибки валидации
                error_details = []
                if isinstance(json_response["errors"], list):
                    for error_item in json_response["errors"]:
                        if isinstance(error_item, list) and len(error_item) == 2:
                            field_name, error_msg = error_item
                            error_details.append(f"{field_name}: {error_msg}")
                raise HTTPException(status_code=400, detail=f"FunPay validation errors: {'; '.join(error_details)}")
        except ValueError:
            pass
        
        return {
            "success": True,
            "message": "Lot created successfully",
            "subcategory_id": subcategory_id,
            "seller_id": account.id,
            "seller_username": account.username
        }
        
    except Exception as e:
        logger.error(f"Error creating lot: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))
@app.post("/copy-lots")
async def copy_lots_endpoint(request: CopyLotsRequest) -> dict:
    try:
        loop = asyncio.get_event_loop()
        with ThreadPoolExecutor(max_workers=4) as executor:
            account = Account(golden_key=request.golden_key)
            await loop.run_in_executor(executor, account.get)
            
            result = await loop.run_in_executor(
                executor, 
                copy_lots_from_subcategory,
                request.user_id,
                request.subcategory_id,
                account
            )
            
        return {"copied_lots": result, "total": len(result)}
    except Exception as e:
        logger.error(f"Error copying lots: {str(e)}")
        raise HTTPException(status_code=400, detail=f"Failed to copy lots: {str(e)}")
async def copy_lots_by_user_id(
    user_id: int, 
    subcategory_id: Optional[int] = None, 
    account: Account = None,
    loop: asyncio.AbstractEventLoop = None,
    executor: ThreadPoolExecutor = None
) -> List[dict]:
    logger.info(f"Copying lots for user ID: {user_id}, subcategory: {subcategory_id}")

    if subcategory_id is None or subcategory_id == -1:
        subcategories_to_process = await loop.run_in_executor(
            executor, get_user_subcategories, user_id, account
        )
        logger.info(f"Found {len(subcategories_to_process)} subcategories for user {user_id}")
    else:
        subcategories_to_process = [subcategory_id]

    all_created_lots = []

    for subcat in subcategories_to_process:
        try:
            lots_from_subcategory = await loop.run_in_executor(
                executor, copy_lots_from_subcategory, user_id, subcat, account
            )
            all_created_lots.extend(lots_from_subcategory)
        except Exception as ex:
            logger.error(f"Error copying lots from subcategory {subcat} for user {user_id}", exc_info=ex)

    logger.info(f"Successfully copied {len(all_created_lots)} lots total for user ID: {user_id}")
    return all_created_lots

@app.get("/get_user_subcategories/{user_id}")
def get_user_subcategories(user_id: int, golden_key: str) -> List[int]:
    try:
        account = Account(golden_key=golden_key)
        account.get()
        user = account.get_user(user_id)
        lots = user.get_lots()
        if not lots:
            logger.info(f"No lots found for user {user_id}")
            return []
        
        subcategories = list(set(
            lot.subcategory.id for lot in lots 
            if lot.subcategory and lot.subcategory.type == SubCategoryTypes.COMMON
        ))
        if not subcategories:
            logger.info(f"No common subcategories found for user {user_id}")
        return subcategories
    except Exception as e:
        logger.error(f"Error getting user subcategories: {e}")
        return []

def copy_lots_from_subcategory(user_id: int, subcat: int, account: Account) -> List[dict]:
    try:
        lots = account.get_subcategory_public_lots(SubCategoryTypes.COMMON, subcat, locale="ru")
        user_lots: List[LotShortcut] = [lot for lot in lots if lot.seller and lot.seller.id == user_id]
        created = []

        for lot in user_lots:
            try:
                lot_page: LotPage = account.get_lot_page(lot.id, locale="ru")
                
                # Получаем подкатегорию для category_name
                subcategory = lot.subcategory if lot.subcategory else account.get_subcategory(SubCategoryTypes.COMMON, subcat)
                
                # Получаем пустые поля для нового лота
                response = account.method("get", f"lots/offerEdit?offer=0&node={subcat}", {}, {}, raise_not_200=True)
                bs = BeautifulSoup(response.content.decode(), "lxml")
                fields = {}
                
                # Извлечение полей формы
                for field in bs.find_all("input"):
                    if "name" in field.attrs:
                        fields[field["name"]] = field.get("value", "")
                
                for field in bs.find_all("textarea"):
                    if "name" in field.attrs:
                        fields[field["name"]] = field.get_text(strip=True)
                
                for field in bs.find_all("select"):
                    if "name" in field.attrs:
                        parent = field.find_parent(class_="form-group")
                        if parent and "hidden" not in parent.get("class", []):
                            selected = field.find("option", selected=True)
                            if selected and "value" in selected.attrs:
                                fields[field["name"]] = selected["value"]
                
                for field in bs.find_all("input", {"type": "checkbox"}):
                    if "name" in field.attrs and field.get("checked"):
                        fields[field["name"]] = "on"

                # Заполняем поля из копируемого лота
                fields["csrf_token"] = account.csrf_token
                fields["offer_id"] = "0"
                fields["node_id"] = str(subcat)
                fields["price"] = str(lot.price)
                fields["fields[summary][ru]"] = lot.description or ""
                fields["fields[summary][en]"] = lot.description or ""
                fields["fields[desc][ru]"] = lot_page.full_description or "" if lot_page else ""
                fields["fields[desc][en]"] = lot_page.full_description or "" if lot_page else ""
                fields["param_0"] = lot.server or ""
                fields["amount"] = str(lot.amount) if lot.amount is not None else ""
                fields["auto_delivery"] = "on" if lot.auto else ""
                fields["fields[attributes]"] = ",".join(f"{k}:{v}" for k, v in (lot.attributes or {}).items())

                # Обработка изображений
                photo_ids = []
                if lot_page and lot_page.image_urls:
                    for idx, img_url in enumerate(lot_page.image_urls):
                        try:
                            response = requests.get(img_url, timeout=10)
                            response.raise_for_status()
                            img_data = response.content
                            photo_id = account.upload_image(img_data, "offer")
                            photo_ids.append(photo_id)
                        except requests.RequestException as e:
                            logger.warning(f"Failed to download image {img_url}: {e}")
                            continue
                        except Exception as e:
                            logger.warning(f"Failed to upload image: {e}")
                            continue
                    
                    for idx, pid in enumerate(photo_ids):
                        fields[f"photos[{idx}]"] = str(pid)

                # Сохраняем новый лот
                new_lot = LotFields(
                    lot_id=0,
                    fields=fields,
                    subcategory=subcategory,
                    currency=lot.currency if lot.currency else "RUB",
                    calc_result=None
                )
                
                account.save_lot(new_lot)
                created.append({
                    "Id": 0,  # ID неизвестен
                    "Server": lot.server or "",
                    "Description": lot.description or "",
                    "Title": lot.title or "",
                    "Amount": lot.amount,
                    "Price": lot.price,
                    "Currency": lot.currency.name if lot.currency else "RUB",
                    "SellerId": account.id,
                    "SellerUsername": account.username,
                    "AutoDelivery": lot.auto,
                    "IsPromo": lot.promo,
                    "Attributes": lot.attributes or {},
                    "SubcategoryId": subcat,
                    "CategoryName": subcategory.category.name if subcategory and subcategory.category else "",
                    "Html": "",  # HTML недоступен для нового лота
                    "PublicLink": f"https://funpay.com/lots/offer?id=0"  # Заглушка
                })
                
            except Exception as e:
                logger.error(f"Error copying individual lot {lot.id}: {e}")
                continue

        return created
    except Exception as e:
        logger.error(f"Error copying lots from subcategory {subcat}: {e}")
        return []
# Добавим endpoint для получения подкategories пользователя
@app.get("/get_user_subcategories/{user_id}")
async def get_user_subcategories_endpoint(user_id: int, golden_key: str):
    try:
        loop = asyncio.get_event_loop()
        with ThreadPoolExecutor() as executor:
            account = Account(golden_key)
            await loop.run_in_executor(executor, account.get)
            
            subcategories = await loop.run_in_executor(
                executor, get_user_subcategories, user_id, account
            )
            
        return {"user_id": user_id, "subcategories": subcategories}
    except Exception as e:
        logger.error(f"Error getting user subcategories: {str(e)}")
        raise HTTPException(status_code=400, detail=str(e))

def parse_lot_fields_from_html(html_content: str) -> dict:
    from bs4 import BeautifulSoup
    bs = BeautifulSoup(html_content, "lxml")
    
    fields = {}
    # Извлекаем все input поля
    for field in bs.find_all("input"):
        if "name" in field.attrs:
            fields[field["name"]] = field.get("value", "")
    
    # Извлекаем textarea
    for field in bs.find_all("textarea"):
        if "name" in field.attrs:
            fields[field["name"]] = field.get_text(strip=True)
    
    # Извлекаем select
    for field in bs.find_all("select"):
        if "name" in field.attrs:
            selected = field.find("option", selected=True)
            if selected:
                fields[field["name"]] = selected.get("value", "")
    
    return fields