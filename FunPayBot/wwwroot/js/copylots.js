$(document).ready(function () {
    var allUserLots = []; // Храним все лоты пользователя

    $('#userId').on('change', function () {
        var input = $(this).val().trim();
        var userId = extractUserId(input);

        if (userId) {
            // Загружаем профиль пользователя
            loadUserProfile(userId);

            // Загружаем ВСЕ лоты пользователя
            loadAllUserLots(userId);
        } else {
            $('#userProfilePreview').hide();
            $('#userLotsSection').hide();
            allUserLots = [];
        }
    });

    function extractUserId(input) {
        if (!input) return null;

        // Проверяем если это URL от FunPay
        if (input.includes('funpay.com/users/')) {
            // Извлекаем ID из URL: https://funpay.com/users/3671641/
            var match = input.match(/funpay\.com\/users\/(\d+)/);
            if (match && match[1]) {
                return match[1];
            }
        }

        // Проверяем если это просто числовой ID
        var numericMatch = input.match(/^\d+$/);
        if (numericMatch) {
            return input;
        }

        // Если ничего не подошло
        return null;
    }
    $('#subcategoryId').on('change', function () {
        if (allUserLots.length > 0) {
            var subcategoryId = extractSubcategoryId($(this).val().trim());
            filterAndDisplayLots(subcategoryId);
        }
    });

    // Кнопка показать/скрыть лоты
    $('#toggleLotsBtn').on('click', function () {
        var container = $('#lotsContainer');
        var text = $('#toggleLotsText');

        if (container.is(':visible')) {
            container.hide();
            text.text('Показать');
        } else {
            container.show();
            text.text('Скрыть');
        }
    });

    function loadUserProfile(userId) {
        $.get('/api/user/' + userId + '/info', function (data) {
            if (data.imageUrl && data.username) {
                $('#userImage').attr('src', data.imageUrl);
                $('#userNameDisplay').text(data.username);
                $('#userProfilePreview').show();
            } else {
                $('#userProfilePreview').hide();
            }
        }).fail(function () {
            $('#userProfilePreview').hide();
        });
    }

    function loadAllUserLots(userId) {
        $('#userLotsSection').show();
        $('#lotsLoading').show();
        $('#lotsList').hide().empty();

        var requestData = {
            userId: parseInt(userId),
            subcategoryId: null
        };

    
        $.ajax({
            url: '/api/get-lots-by-userid',
            method: 'POST',
            contentType: 'application/json',  
            data: JSON.stringify(requestData),  
            success: function (data) {
                $('#lotsLoading').hide();
                if (data && data.length > 0) {
                    allUserLots = data;
                    var subcategoryId = extractSubcategoryId($('#subcategoryId').val().trim());
                    filterAndDisplayLots(subcategoryId);
                } else {
                    allUserLots = [];
                    $('#lotsList').html('<p class="text-muted text-center">У пользователя нет лотов</p>').show();
                }
            },
            error: function (xhr) {
                $('#lotsLoading').hide();
                allUserLots = [];
                var errorMessage = xhr.status === 404 ? 'Лоты не найдены' : 'Ошибка загрузки лотов';
                $('#lotsList').html('<p class="text-danger text-center">' + errorMessage + '</p>').show();
            }
        });
    }
    function filterAndDisplayLots(subcategoryId) {
        var filteredLots = allUserLots;

        // Фильтруем по подкатегории если указана
        if (subcategoryId) {
            filteredLots = allUserLots.filter(function (lot) {
                return lot.subcategoryId == subcategoryId;
            });
        }

        displayLots(filteredLots, subcategoryId);
    }

    function displayLots(lots, subcategoryId) {
        if (lots.length === 0) {
            var message = subcategoryId
                ? 'У пользователя нет лотов в этой категории'
                : 'У пользователя нет лотов';
            $('#lotsList').html('<p class="text-muted text-center">' + message + '</p>').show();
            return;
        }

        var lotsHtml = '<div class="row g-2">';

        lots.forEach(function (lot) {
            lotsHtml += `
                <div class="col-md-6 col-lg-4">
                    <div class="card card-sm">
                        <div class="card-body p-2">
                            <h6 class="card-title mb-1" title="${lot.description}">${truncateText(lot.description, 150)}</h6>
                            <p class="card-text small text-muted mb-1">Цена: <strong>${lot.price}</strong></p>
                            <p class="card-text small text-muted mb-1">Категория: ${lot.subcategoryId || 'Не указана'}</p>
                            <p class="card-text small text-muted mb-0">ID: ${lot.id}</p>
                        </div>
                    </div>
                </div>
            `;
        });

        lotsHtml += '</div>';

        var filterText = subcategoryId ? ` (отфильтровано по категории ${subcategoryId})` : '';
        lotsHtml += `<p class="text-center text-muted mt-3 small">
            Показано лотов: ${lots.length} из ${allUserLots.length}${filterText}
        </p>`;

        $('#lotsList').html(lotsHtml).show();
    }

    function extractUserId(input) {
        if (!input) return null;

        if (input.includes('funpay.com/users/')) {
            var match = input.match(/funpay\.com\/users\/(\d+)/);
            if (match && match[1]) return match[1];
        }

        if (/^\d+$/.test(input)) {
            return input;
        }

        return null;
    }

    function extractSubcategoryId(input) {
        if (!input) return null;

        if (input.includes('funpay.com')) {
            var match = input.match(/\/(\d+)\/?$/);
            if (match && match[1]) return match[1];
        }

        if (/^\d+$/.test(input)) {
            return input;
        }

        return null;
    }

    function truncateText(text, maxLength) {
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    }
});