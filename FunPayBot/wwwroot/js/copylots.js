$(document).ready(function () {
    $('#userId').on('change', function () {
        var input = $(this).val().trim();
        var userId = extractUserId(input); // твоя функция извлечения ID

        if (userId) {
            $.get('/api/user/' + userId + '/info', function (data) {
                if (data.imageUrl && data.username) {
                    $('#userImage').attr('src', data.imageUrl);
                    $('#userNameDisplay').text(data.username);
                    $('#userProfilePreview').show(); // Показываем блок профиля
                } else {
                    $('#userProfilePreview').hide();
                }
            }).fail(function () {
                $('#userProfilePreview').hide();
            });
        } else {
            $('#userProfilePreview').hide();
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
});