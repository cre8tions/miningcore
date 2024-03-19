<?php
// src/Controller/LuckyController.php
/**
 * Namespace declaration for the LuckyController class.
 *
 * This namespace declaration specifies the location of the LuckyController class
 * within the application's directory structure.
 *
 * @package App\Controller
 */
namespace App\Controller;

use Symfony\Component\HttpFoundation\Response;
use Symfony\Component\Routing\Annotation\Route;
use Symfony\Bundle\FrameworkBundle\Controller\AbstractController;

/**
 * Controller class for generating a lucky number.
 */
class IndexController extends AbstractController
{
    /**
     * Generates a random lucky number.
     *
     * @return Response The HTTP response containing the lucky number.
     */
    #[Route('/')]
    public function index(): Response
    {
        return $this->render('index.html.twig', []);
    }

    #[Route('/support')]
    public function support(): Response
    {
        return $this->render('support.html.twig', []);
    }
}