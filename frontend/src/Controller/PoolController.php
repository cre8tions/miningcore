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
use Symfony\Component\HttpFoundation\Request;
use Symfony\Component\Routing\Annotation\Route;
use Symfony\Bundle\FrameworkBundle\Controller\AbstractController;

/**
 * Controller class for generating a lucky number.
 */
class PoolController extends AbstractController
{
    /**
     * Generates a random lucky number.
     *
     * @return Response The HTTP response containing the lucky number.
     */
    #[Route('/pool/{poolId}')]
    public function index(string $poolId, Request $request): Response
    {
        $session = $request->getSession();
        if (!$session->get('pool_Id'))  {
            $session->set('pool_Id', $request->attributes->get('poolId'));
        }

        return $this->render('pool/index.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }

    #[Route('/pool/{poolId}/dash', name: 'poolDash')]
    public function dash(string $poolId, Request $request): Response
    {
        return $this->render('pool/dash.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }

    #[Route('/pool/{poolId}/dash/{wallet}', name: 'poolDashWallet')]
    public function dashWallet(string $poolId, Request $request): Response
    {
        return $this->render('pool/dashwallet.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
            'wallet' => $request->attributes->get('wallet')
        ]);
    }

    #[Route('/pool/{poolId}/miners', name: 'poolMiners')]
    public function miners(string $poolId, Request $request): Response
    {
        return $this->render('pool/miners.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }

    #[Route('/pool/{poolId}/blocks', name: 'poolBlocks')]
    public function blocks(string $poolId, Request $request): Response
    {
        return $this->render('pool/blocks.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }

    #[Route('/pool/{poolId}/payments', name: 'poolPayments')]
    public function payments(string $poolId, Request $request): Response
    {
        return $this->render('pool/payments.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }

    #[Route('/pool/{poolId}/connect', name: 'poolConnect')]
    public function connect(string $poolId, Request $request): Response
    {
        return $this->render('pool/connect.html.twig', [
            'pool_Id' => $request->attributes->get('poolId'),
        ]);
    }
}