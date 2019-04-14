function addToCart(productId) {
    $(document).ready(function () {
        $.ajax({
            url: "/OrderDetails/AjaxAddToCart?ProductId=" + productId,
            success: function (result) {
                result = JSON.parse(result);
                console.log(result);
                if (result.error == '0') {
                    var gid = ".pic_img_" + productId;
                    var cart = $('#cart');
                    if (cart.length > 0) {
                        var flyElm = $(gid).clone().css('opacity', '0.7');
                        flyElm.css({
                            'z-index': 9000,
                            'display': 'block',
                            'position': 'absolute',
                            'top': $(gid).offset().top + 'px',
                            'left': $(gid).offset().left + 'px',
                            'width': $(gid).width() + 'px',
                            'height': $(gid).height() + 'px',
                            '-moz-border-radius': 100 + 'px',
                            'border-radius': 100 + 'px',
                            'border-width': 1 + 'px',
                            'border-color': '#333',
                            'border-style': 'solid',
                            'text-align': 'center'
                        });
                        $('body').append(flyElm);
                        $('.cart-count').html(result.totalNum);
                        flyElm.animate({
                            top: $(cart).offset().top,
                            left: $(cart).offset().left,
                            width: 20,
                            height: 20
                        }, 'slow', function () {
                            flyElm.remove();
                        });
                    }
                }
            }
        });
    });
}